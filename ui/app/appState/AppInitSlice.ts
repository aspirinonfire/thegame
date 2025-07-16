import type { StateCreator } from "zustand";
import { isApiError } from "~/common-components/apiError";
import type { Game } from "~/game-core/models/Game";
import type { PlayerInfo } from "~/game-core/UserAccount";
import type { AppStore } from "./AppStore";

// store rehydrate 'resolve' externally so we can resolve parent promise as part of the event
export let rehydrationPromiseResolve: (() => void) | null = null;
export const rehydrationPromise = new Promise<void>(resolve => {
  rehydrationPromiseResolve = resolve;
});

export interface AppInitSlice {
  _hasStorageHydrated: boolean;
  isInitialized: boolean;
  isGsiSdkReady: boolean;

  _setStorageHydrated: (state: boolean) => void;
  initialize: () => Promise<void>;
  retrievePlayerData: () => Promise<boolean>;
}

export const createAppInitSlice: StateCreator<AppStore, [], [], AppInitSlice> = (set, get) => ({
  _hasStorageHydrated: false,
  isInitialized: false,
  isGsiSdkReady: false,

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

  retrievePlayerData: async () => {
    const apiGet = get().get;
    let isSuccessfulRetrieval = true;

    const [playerResult, gamesResult] = await Promise.all([
      apiGet<PlayerInfo>("user"),
      apiGet<Game[]>("game?isActive=true")
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
  }
});

