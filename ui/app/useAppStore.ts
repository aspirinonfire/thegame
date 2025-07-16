import { create, type StateCreator } from "zustand";
import { createJSONStorage, devtools, persist } from "zustand/middleware";
import type UserAccount from "./game-core/UserAccount";
import type { Game } from "./game-core/models/Game";
import type { ScoreData } from "./game-core/models/ScoreData";
import type { LicensePlateSpot } from "./game-core/models/LicensePlateSpot";
import CalculateScore from "./game-core/gameScoreCalculator";
import { isApiError, type apiError } from "./common-components/apiError";
import { type PlayerInfo } from "./game-core/UserAccount";


interface AppState {
  _hasStorageHydrated: boolean,
  
  isInitialized: boolean,

  isGsiSdkReady: boolean,

  activeUser: UserAccount | null,

  activeGame: Game | null,

  pastGames: Game[],

  apiErrors: apiError[]

  apiAccessToken: string | null
}

interface AppActions {
  _setStorageHydrated: (state: boolean) => void,
  waitForRehydration: () => Promise<void>,
  
  initialize: () => Promise<void>,

  authenticateWithGoogleAuthCode: (authCode: string) => Promise<boolean>,

  retrievePlayerData: () => Promise<boolean>,

  startNewGame: (name: string) => Promise<Game | string>,

  spotNewPlates: (spottedPlates: LicensePlateSpot[]) => Promise<Game | string>,

  finishCurrentGame: () => Promise<string | void>,

  api: {
    enqueueError: (apiError: apiError) => void;
    dequeueError: () => apiError | null;
    
    retrieveAccessToken: () => Promise<string | null>;
    refreshAccessToken: () => Promise<string | null>;
    sendAuthenticatedRequest: <TBody, TResponse>(endpoint: string, method: string, body: TBody | null, includeCreds: boolean) => Promise<TResponse | apiError>;
    sendUnauthenticatedRequest: <TBody, TResponse>(url: string, method: string, body: TBody | null, includeCreds: boolean) => Promise<TResponse | apiError>;
    get: <TResponse>(endpoint: string) => Promise<TResponse | apiError>;
    post: <TResponse, TBody = void>(endpoint: string, body?: TBody) => Promise<TResponse | apiError>;
  }
};

interface ApiTokenResponse {
  accessToken: string
}

const mockDataAccessDelay = async () => {
  await new Promise(resolve => setTimeout(resolve, 200));
};

// store rehydrate 'resolve' externally so we can resolve parent promise as part of the event
let rehydrationPromiseResolve: (() => void) | null = null;
const rehydrationPromise = new Promise<void>(resolve => {
  rehydrationPromiseResolve = resolve;
});

const createStore: StateCreator<AppState & AppActions> = (set, get) => ({
  // app state
  _hasStorageHydrated: false,
  isInitialized: false,
  isGsiSdkReady: false,
  activeUser: null,
  activeGame: null,
  pastGames: [],
  isMigratedFromNextJs: false,
  apiErrors: [],
  apiAccessToken: null,

  // app actions
  _setStorageHydrated: (state: boolean) => {
    set({_hasStorageHydrated: true});
    
    // resolve rehydrate promise if applicable
    if (state && rehydrationPromiseResolve) {
      rehydrationPromiseResolve();
      rehydrationPromiseResolve = null;
    }
  },

  waitForRehydration: () => rehydrationPromise,

  initialize: async () => {
    // Wait for rehydration before initializing
    await get().waitForRehydration();

    await mockDataAccessDelay();

    if (!get().apiAccessToken) {
      set({
        activeUser: {
          player: {
            playerId: -1,
            playerName: "Guest User",
          },
          isAuthenticated: false
        }
      });
    } else {
      await get().retrievePlayerData();
    }

    set({ isInitialized: true});

    if (get().isGsiSdkReady) {
      return;
    }

    const existing = document.querySelector<HTMLScriptElement>(
      'script[src="https://accounts.google.com/gsi/client"]'
    );
    const script = existing ?? document.createElement('script');

    const onLoad = () => set({ isGsiSdkReady: true });
    const onError = () => alert("Failed to load Google Sign-In SDK. Please try again later.");

    if (!existing) {
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.addEventListener('load', onLoad, { once: true });
      script.addEventListener('error', onError, { once: true });
      document.head.appendChild(script);
    } else {
      // tag was already there but might have loaded while we weren’t listening
      if ((window as any).google) {
        onLoad();
      } 
      else {
        existing.addEventListener('load', onLoad, { once: true });
        existing.addEventListener('error', onError, { once: true });
      }
    }
  },

  startNewGame: async (name: string) => {
    if (get().activeGame) {
      console.error("Only one active game is allowed!");
      return "Only one active game is allowed!";
    }

    const newGame = {
      dateCreated: new Date(),
      createdByPlayerId: get().activeUser?.player.playerId ?? -1,
      createdByPlayerName: get().activeUser?.player.playerName ?? "N/A",
      gameId: new Date().getTime(),
      spottedPlates: [],
      gameName: name,
      score: <ScoreData>{
        totalScore: 0,
        milestones: []
      }
    }; 

    set({
      activeGame: newGame
    });
    
    return newGame;
  },

  spotNewPlates: async (spottedPlates) => {
    const currentGame = get().activeGame;
    if (!currentGame) {
      console.error("No Active Game!");
      return "No active game!";
    }

    const updatedGame = <Game>{...currentGame,
      spottedPlates: spottedPlates,
      score: CalculateScore(spottedPlates)
    };

    set({
      activeGame: updatedGame
    });

    return updatedGame;
  },

  finishCurrentGame: async () => {
    const currentGame = get().activeGame;
    if (!currentGame) {
      console.error("No Active Game!");
      return "No active game!";
    }

    // TODO implement!!
    const endedGame = await get().api.post<Game>(`game/${currentGame.gameId}/endgame`);
    return;

    // use last spot as date finished
    const lastSpot = currentGame.spottedPlates
      .map(plate => plate.spottedOn)
      .filter(date => !!date)
      .sort()
      .at(-1);

    if (!!lastSpot) {
      currentGame.endedOn = lastSpot;

      const pastGames = get().pastGames;
      pastGames.push(currentGame);

      set({
        pastGames: pastGames,
      });
    }

    set({
      activeGame: null
    });
  },

  authenticateWithGoogleAuthCode: async (authCode: string) => {
    const accessTokenResponse = await get().api.sendUnauthenticatedRequest<string, ApiTokenResponse>(
      "user/google/apitoken",
      "POST",
      authCode,
      true
    )

    if (!isApiError(accessTokenResponse)) {
      set({
        apiAccessToken: accessTokenResponse.accessToken,
      });

      return true;
    }

    return false;
  },

  retrievePlayerData: async () => {
    const api = get().api;
    let isSuccessfulRetrieval = true;

    const [playerResult, gamesResult] = await Promise.all([
      api.get<PlayerInfo>("user"),
      api.get<Game[]>("game?isActive=true")
    ]);

    if (isApiError(playerResult)) {
      isSuccessfulRetrieval = false;
    } else {
      set({
        activeUser: {
          isAuthenticated: true,
          player: playerResult
        }
      });
    }

    if (isApiError(gamesResult)) {
      isSuccessfulRetrieval = false;
    } else {
      set({
        activeGame: gamesResult[0]
      });
    }

    return isSuccessfulRetrieval;
  },

  api: {
    enqueueError: (apiError: apiError) => {
      set((s) => ({ apiErrors: [...s.apiErrors, apiError]}))
    },

    dequeueError: () => {
      const allErrors = get().apiErrors;

      set((s) => ({ apiErrors: allErrors.slice(1) }))

      return allErrors[0];
    },

    retrieveAccessToken: async () => {
      return get().apiAccessToken;
    },
    
    refreshAccessToken: async () => {
      // TODO need to retrieve ID Token so we can confirm API and OAuth session are for the same identity.
      // TODO consider merging with retrieveAccessToken. This will need tracking of token expiration so we can do silent refresh.
      const currentAccessToken = get().apiAccessToken;

      const refreshResponse = await get().api.sendUnauthenticatedRequest<any, ApiTokenResponse>("user/refresh-token",
        "POST",
        {
          accessToken: currentAccessToken,
          idToken: "id-token-here-wip",
          identityProvider: "Google"
        },
        true
      );

      if (!isApiError(refreshResponse)) {
        set({
          apiAccessToken: refreshResponse.accessToken
        });
        return refreshResponse.accessToken;
      }

      return null;
    },

    sendUnauthenticatedRequest: async <TBody, TResponse>(endpoint: string, method: string, body: TBody | null, includeCreds: boolean) => {
      const normalizedEndpointUrl = (endpoint || '').replace(/^\//, '');
      const apiResponse = await fetch(`${import.meta.env.VITE_API_URL}/api/${normalizedEndpointUrl}`, {
        cache: "no-cache",
        method: method,
        body: body ? JSON.stringify(body) : null,
        headers: {
          "Content-Type": "application/json; charset=utf-8",
        },
        credentials: includeCreds ? "include" : undefined
      });

      if (apiResponse.ok) {
        // Parse the response JSON into the expected TResponse type
        const data: TResponse = await apiResponse.json();
        return data;
      }

      const errorData: apiError = {
        status: apiResponse.status,
        title: 'Failed to send request.',
        detail: await apiResponse.text(),
        GameRequestCorrelationId: '',
        traceId: ''
      }

      get().api.enqueueError(errorData);
      return errorData;
    },

    sendAuthenticatedRequest: async <TBody, TResponse>(endpoint: string, method: string, body: TBody | null, includeCreds: boolean) => {
      let accessToken = await get().api.retrieveAccessToken();
      
      if (!accessToken) {
        const errorData: apiError = {
          status: 401,
          title: 'Failed to retrieve Access Token.',
          detail: 'Please contact IT Support for assistance.',
          GameRequestCorrelationId: '',
          traceId: ''
        }

        get().api.enqueueError(errorData);

        return errorData;
      }

      const makeRequest = async (bearerToken: string) => {
        const normalizedEndpointUrl = (endpoint || '').replace(/^\//, '');
        const apiResponse = await fetch(`${import.meta.env.VITE_API_URL}/api/${normalizedEndpointUrl}`, {
          cache: "no-cache",
          method: method,
          headers: {
            "Authorization": `Bearer ${bearerToken}`,
            "Content-Type": "application/json; charset=utf-8",
          },
          body: body ? JSON.stringify(body) : null,
          credentials: includeCreds ? "include" : undefined
        });

        return apiResponse;
      };

      let apiResponse = await makeRequest(accessToken);

      if (apiResponse.status == 401) {
        accessToken = await get().api.refreshAccessToken();

        if (!accessToken) {
          const errorData: apiError = {
            status: 401,
            title: 'Failed to retrieve Access Token.',
            detail: 'Please contact IT Support for assistance.',
            GameRequestCorrelationId: '',
            traceId: ''
          }

          get().api.enqueueError(errorData);

          return errorData;
        }

        apiResponse = await makeRequest(accessToken);
      }
      
      if (apiResponse.ok) {
        // Parse the response JSON into the expected TResponse type
        const data: TResponse = await apiResponse.json();
        return data;
      }
      
      // API errors return standard rfc9110 payload
      const errorData: apiError = await apiResponse.json();
      get().api.enqueueError(errorData);
      return errorData;
    },

    get: async <TResponse>(endpoint: string) =>
      await get().api.sendAuthenticatedRequest<unknown, TResponse>(endpoint, "get", null, false),

    post: async <TBody, TResponse>(endpoint: string, body: TBody) =>
      await get().api.sendAuthenticatedRequest<TBody, TResponse>(endpoint, "post", body, false) 
  }
});

export const useAppStore = create<AppState & AppActions>()(
  devtools(
    persist(createStore,
      {
        name: "Game UI",
        storage: createJSONStorage(() => localStorage),
        
        partialize: (state) => ({
          activeGame: state.activeGame,
          pastGames: state.pastGames,
          apiAccessToken: state.apiAccessToken
        }),

        onRehydrateStorage: (state) => {
          return () => state._setStorageHydrated(true)
        }
      }
    )
  )
)