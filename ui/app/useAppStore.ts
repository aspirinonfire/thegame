import { create, type StateCreator } from "zustand";
import { createJSONStorage, devtools, persist } from "zustand/middleware";
import type UserAccount from "./game-core/UserAccount";
import type { Game } from "./game-core/models/Game";
import type { ScoreData } from "./game-core/models/ScoreData";
import type { LicensePlateSpot } from "./game-core/models/LicensePlateSpot";
import CalculateScore from "./game-core/gameScoreCalculator";
import { deleteNextJsGameData, retrieveNextJsData } from "./game-core/migrations/nextjs-game-repository";
import type { NextJsGame } from "./game-core/migrations/nextjs-models";

interface AppState {
  _hasStorageHydrated: boolean,
  isInitialized: boolean,

  isMigratedFromNextJs: boolean,

  activeUser: UserAccount | null,

  activeGame: Game | null

  pastGames: Game[]
}

interface AppActions {
  _setStorageHydrated: (state: boolean) => void,
  waitForRehydration: () => Promise<void>,
  
  initialize: () => Promise<void>,

  startNewGame: (name: string) => Promise<Game | string>,

  spotNewPlates: (spottedPlates: LicensePlateSpot[]) => Promise<Game | string>,

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

const getNewGameFromNextJsGame = (oldGame: NextJsGame, activeUser: UserAccount | null) : Game => ({
  id: oldGame.id,
  name: oldGame.name,
  score: {
    totalScore: oldGame.score?.totalScore ?? 0,
    milestones: oldGame.score?.milestones ?? []
  },
  createdBy: activeUser?.name ?? oldGame.createdBy,
  dateCreated: oldGame.dateCreated,
  dateFinished: oldGame.dateFinished,
  licensePlates: Object.values(oldGame.licensePlates ?? {})
    .filter(spot => !!spot.dateSpotted)
    .map(spot => ({
      key: `${spot.country}-${spot.stateOrProvince}`,
      dateSpotted: spot.dateSpotted,
      spottedBy: activeUser?.name ?? spot.spottedBy
    } as LicensePlateSpot))
})

const getNextJsDataAsNew = (activeUser: UserAccount | null) => {
  const oldData = retrieveNextJsData();


  const currentGame: Game | null = !!oldData.currentGame ?
    getNewGameFromNextJsGame(oldData.currentGame, activeUser):
    null;

  const pastGames: Game[] = (oldData.pastGames ?? [])
    .map(game => getNewGameFromNextJsGame(game, activeUser));

  return {
    currentGame,
    pastGames
  }
}

const createStore: StateCreator<AppState & AppActions> = (set, get) => ({
  // app state
  _hasStorageHydrated: false,
  isInitialized: false,
  activeUser: null,
  activeGame: null,
  pastGames: [],
  isMigratedFromNextJs: false,

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

    if (!get().isMigratedFromNextJs) {
      const dataToInsert = getNextJsDataAsNew(get().activeUser);

      set({
        activeGame: dataToInsert.currentGame,
        pastGames: dataToInsert.pastGames.concat(get().pastGames),
        isMigratedFromNextJs: true
      });

      deleteNextJsGameData();
    }

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
      licensePlates: [],
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

  spotNewPlates: async (spottedPlates) => {
    const currentGame = get().activeGame;
    if (!currentGame) {
      console.error("No Active Game!");
      return "No active game!";
    }

    const updatedGame = <Game>{...currentGame,
      licensePlates: spottedPlates,
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
    const lastSpot = currentGame.licensePlates
      .map(plate => plate.dateSpotted)
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
          pastGames: state.pastGames,
          isMigratedFromNextJs: state.isMigratedFromNextJs
        }),

        onRehydrateStorage: (state) => {
          return () => state._setStorageHydrated(true)
        }
      }
    )
  )
)