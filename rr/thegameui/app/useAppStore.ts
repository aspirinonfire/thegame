import { create, type StateCreator } from "zustand";
import { createJSONStorage, devtools, persist } from "zustand/middleware";
import type UserAccount from "./game-core/UserAccount";
import type { Game } from "./game-core/models/Game";
import type { ScoreData } from "./game-core/models/ScoreData";

interface AppState {
  _hasStorageHydrated: boolean,
  isInitialized: boolean,

  activeUser: UserAccount | null,

  activeGame: Game | null

  pastGames: Game[]
}

interface AppActions {
  _setStorageHydrated: (state: boolean) => void,
  waitForRehydration: () => Promise<void>,
  
  initialize: () => Promise<void>,

  startNewGame: (name: string) => Promise<Game | string>,

  finishCurrentGame: () => Promise<string | void>,
};

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
  activeUser: null,
  activeGame: null,
  pastGames: [],

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

    set({
      activeUser: {
        name: "Guest User"
      },
      isInitialized: true
    });
  },

  startNewGame: async (name: string) => {
    if (get().activeGame) {
      console.error("Only one active game is allowed!");
      return "Only one active game is allowed!";
    }

    const newGame = {
      dateCreated: new Date(),
      createdBy: get().activeUser?.name ?? "N/A",
      id: new Date().getTime().toString(),
      licensePlates: {},
      name: name,
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

  finishCurrentGame: async () => {
    const currentGame = get().activeGame;
    if (!currentGame) {
      console.error("No Active Game!");
      return "No active game!";
    }

    // use last spot as date finished
    const lastSpot = Object.keys(currentGame.licensePlates)
      .map(key => currentGame.licensePlates[key].dateSpotted)
      .filter(date => !!date)
      .sort()
      .at(-1);

    if (!!lastSpot) {
      currentGame.dateFinished = lastSpot;

      const pastGames = get().pastGames;
      pastGames.push(currentGame);

      set({
        pastGames: pastGames,
      });
    }

    set({
      activeGame: null
    });
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
          pastGames: state.pastGames
        }),

        onRehydrateStorage: (state) => {
          return () => state._setStorageHydrated(true)
        }
      }
    )
  )
)