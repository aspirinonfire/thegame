import { create } from "zustand";
import { createJSONStorage, devtools, persist } from "zustand/middleware";
import { createAppInitSlice } from "./AppInitSlice";
import { createApiSlice } from "./ApiSlice";
import { createAppAuthSlice } from "./AppAuthSlice";
import { createGameSlice } from "./GameSlice";
import type { AppStore } from "./AppStore";

export const useAppState = create<AppStore>()(
  devtools(
    persist((...args) => ({
      ...createApiSlice(...args),
      ...createAppAuthSlice(...args),
      ...createAppInitSlice(...args),
      ...createGameSlice(...args)
    }),
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