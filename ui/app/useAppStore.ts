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
    
    sendAuthenticatedRequest: <TBody, TResponse>(endpoint: string, method: string, body: TBody | null) => Promise<TResponse | apiError>;
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
      const requestParams : RequestInit = {
        cache: "no-cache",
        method: "POST",
        headers: {
          "Content-Type": "application/json; charset=utf-8",
        },
        body: JSON.stringify(authCode),
      };
  
      try {
        const accessTokenResponse = await fetch(`${import.meta.env.VITE_API_URL}/api/user/google/apitoken`, requestParams);
  
        const responseBody = await accessTokenResponse.json() as ApiTokenResponse;
    
        if (accessTokenResponse.status == 200) {
          set({
            apiAccessToken: responseBody.accessToken,
          });

          return true;
        }
        // TODO generic error handling
        console.error(`Failed to retrieve API token ${accessTokenResponse.status}: ${responseBody}`);
      } catch (error) {
        console.log(error);
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

    sendAuthenticatedRequest: async <TBody, TResponse>(endpoint: string, method: string, body: TBody | null) => {
      const accessToken = get().apiAccessToken;
      
      if (!accessToken) {
        const errorData: apiError = {
          status: 401,
          title: 'Failed to retrieve Access Token.',
          detail: 'Please contact IT Support for assistance.',
          CorrelationId: '',
          traceId: ''
        }

        get().api.enqueueError(errorData);

        return errorData;
      }

      const normalizedEndpointUrl = (endpoint || '').replace(/^\//, '');
      const apiResponse = await fetch(`${import.meta.env.VITE_API_URL}/api/${normalizedEndpointUrl}`, {
        method: method,
        headers: {
          'Authorization': `Bearer ${accessToken}`
        },
        body: body ? JSON.stringify(body) : null
      });

      // API errors return standard rfc9110 payload
      if (!apiResponse.ok) {
        // const errorData: apiError = await apiResponse.json();
        const errorData: apiError = {
          status: apiResponse.status,
          title: 'API request did not succeed.',
          detail: 'Please contact IT Support for assistance.',
          CorrelationId: '',
          traceId: ''
        };
        get().api.enqueueError(errorData);
        return errorData;
      }
    
      // Parse the response JSON into the expected TResponse type
      const data: TResponse = await apiResponse.json();
      return data;
    },

    get: async <TResponse>(endpoint: string) =>
      await get().api.sendAuthenticatedRequest<unknown, TResponse>(endpoint, "get", null),

    post: async <TBody, TResponse>(endpoint: string, body: TBody) =>
      await get().api.sendAuthenticatedRequest<TBody, TResponse>(endpoint, "post", body) 
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