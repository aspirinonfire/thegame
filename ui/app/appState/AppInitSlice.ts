import type { StateCreator } from "zustand";
import type { AppStore } from "./AppStore";
import { guestUser } from "./AppAuthSlice";
import { isApiError } from "./ApiError";
import type { PlayerInfo } from "./UserAccount";
import type { Game } from "~/game-core/models/Game";
import { type WindowWithGoogle } from "./GoogleAuthService";
import { onnxModel } from "./AppAiSearchSlice";

export interface PlayerData {
  player: PlayerInfo | null,
  activeGames: Game[]
}

export interface AppInitSlice {
  isInitialized: boolean;

  retrievePlayerData: () => Promise<boolean>;
  _setStorageHydrated: (state: boolean) => void;
  initialize: () => Promise<void>;
  resetSessionState: () => void;
}

export const createAppInitSlice: StateCreator<AppStore, [], [], AppInitSlice> = (set, get) => {
  // store rehydrate 'resolve' externally so we can resolve parent promise as part of the event
  let rehydrationPromiseResolve: (() => void) | null = null;
  const rehydrationPromise = new Promise<void>(resolve => {
    rehydrationPromiseResolve = resolve;
  });

  return {
    isInitialized: false,

    _setStorageHydrated: (state: boolean) => {
      // resolve rehydrate promise if applicable
      if (state && rehydrationPromiseResolve) {
        rehydrationPromiseResolve();
        rehydrationPromiseResolve = null;
      }
    },

    retrievePlayerData: async () => {
      const playerData = await get().apiGet<PlayerData>("user/userData")

      if (isApiError(playerData) || !playerData.player) {
        console.error("Failed to retrieve player data!");
        return false
      }
      
      set({
        activeUser: {
          isAuthenticated: true,
          player: playerData.player
        },
        activeGame: playerData.activeGames[0]
      });

      return true;
    },

    initialize: async () => {
      // Wait for rehydration before initializing
      await rehydrationPromise;

      if (!get().apiAccessToken) {
        set({
          activeUser: guestUser
        });
      } else {
        const isDataRetrieved = await get().retrievePlayerData();
        console.log("User data initialized: ", isDataRetrieved);
      }

      await get().plateClassifier.init(onnxModel)

      set({ isInitialized: true });

      if (get().isGsiSdkReady) {
        return;
      }

      if (!import.meta.env.VITE_GOOGLE_CLIENT_ID) {
        console.error("Google Client ID is missing. Auth is will not work!");
        return;
      }

      // TODO simplify this mess!
      const existing = document.querySelector<HTMLScriptElement>(
        'script[src="https://accounts.google.com/gsi/client"]'
      );
      const script = existing ?? document.createElement('script');

      const onLoad = () => {
        const google = (window as WindowWithGoogle).google;

        if (!google) {
          console.error("Google SDK did not load!!");
          return;
        }

        const client = google.accounts.oauth2.initCodeClient({
          client_id: import.meta.env.VITE_GOOGLE_CLIENT_ID,
          scope: 'openid email profile',
          ux_mode: 'popup',
          auto_select: true,
          callback: ({ code }) => {
            get().processGoogleAuthCode(code)
              .then(_ => {
                return get().retrievePlayerData()
              })
              .finally(() => {
                set({
                  isProcessingLogin: false
                });
              });
          },
          error_callback: (err) => {
            set({
              isProcessingLogin: false
            });
            console.error(err)  // handles user-closed popup etc.
          },
        });

        set({
          isGsiSdkReady: true,
          googleCodeClient: client
        });
      };
      
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

    resetSessionState: () => {
      set({
        activeUser: guestUser,
        activeGame: null,
        apiAccessToken: null,
        gameHistory: {
          numberOfGames: 0,
          spotStats: {}
        }
      });
    }
  }
}

