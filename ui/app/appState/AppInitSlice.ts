import type { StateCreator } from "zustand";
import type { AppStore } from "./AppStore";
import type { WindowWithGoogle } from "./AppAuthSlice";

// store rehydrate 'resolve' externally so we can resolve parent promise as part of the event
export let rehydrationPromiseResolve: (() => void) | null = null;
export const rehydrationPromise = new Promise<void>(resolve => {
  rehydrationPromiseResolve = resolve;
});

export interface AppInitSlice {
  _hasStorageHydrated: boolean;
  isInitialized: boolean;

  _setStorageHydrated: (state: boolean) => void;
  initialize: () => Promise<void>;
}

export const createAppInitSlice: StateCreator<AppStore, [], [], AppInitSlice> = (set, get) => ({
  _hasStorageHydrated: false,
  isInitialized: false,

  _setStorageHydrated: (state: boolean) => {
    set({ _hasStorageHydrated: true });

    // resolve rehydrate promise if applicable
    if (state && rehydrationPromiseResolve) {
      rehydrationPromiseResolve();
      rehydrationPromiseResolve = null;
    }
  },

  initialize: async () => {
    // Wait for rehydration before initializing
    await rehydrationPromise;

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

    set({ isInitialized: true });

    if (get().isGsiSdkReady) {
      return;
    }

    if (!import.meta.env.VITE_GOOGLE_CLIENT_ID) {
      console.error("Google Client ID is missing. Auth is will not work!");
      return;
    }

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
        googleSdkClient: client
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
  }  
});

