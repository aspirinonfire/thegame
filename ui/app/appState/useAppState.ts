import { create } from "zustand";
import { createJSONStorage, devtools, persist } from "zustand/middleware";
import { createAppInitSlice } from "./AppInitSlice";
import { createAppApiSlice } from "./AppApiSlice";
import { createAppAuthSlice } from "./AppAuthSlice";
import { createAppGameSlice } from "./AppGameSlice";
import type { AppStore } from "./AppStore";
import { createAppAiSearchSlice } from "./AppAiSearchSlice";

export const useAppState = create<AppStore>()(
  devtools(
    persist((...args) => ({
      ...createAppApiSlice(...args),
      ...createAppAuthSlice(...args),
      ...createAppInitSlice(...args),
      ...createAppGameSlice(...args),
      ...createAppAiSearchSlice(...args)
    }),
    {
      name: "Game UI",
      storage: createJSONStorage(() => localStorage),
      
      partialize: (state) => ({
        activeGame: state.activeGame,
        apiAccessToken: state.apiAccessToken
      }),

      onRehydrateStorage: (state) => {
        return () => state._setStorageHydrated(true)
      }
    }
    )
  )
)