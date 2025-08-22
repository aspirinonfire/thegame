import type { StateCreator } from "zustand";
import type { Game } from "~/game-core/models/Game";
import type { LicensePlateSpot } from "~/game-core/models/LicensePlateSpot";
import type { AppStore } from "./AppStore";
import { isApiError } from "~/appState/ApiError";

export interface GameHistory {
  numberOfGames: number,
  spotStats: { [key: string]: number }
}

export interface AppGameSlice {
  activeGame: Game | null;
  gameHistory: GameHistory;

  startNewGame: (name: string) => Promise<Game | null>;
  spotNewPlates: (spottedPlates: LicensePlateSpot[]) => Promise<Game | null>;
  finishCurrentGame: () => Promise<void | null>;
  retrieveGameHistory: () => Promise<GameHistory | null>;
}

export const createAppGameSlice: StateCreator<AppStore, [], [], AppGameSlice> = (set, get) => ({
  activeGame: null,
  gameHistory: {
    numberOfGames: 0,
    spotStats: {}
  },
  
  startNewGame: async (name: string) => {
    if (get().activeGame) {
      console.error("Only one active game is allowed!");
      return null;
    }

    const newGameRequest = {
      newGameName: name
    };

    const newGameResult = await get().apiPost<Game>("game", newGameRequest);

    if (isApiError(newGameResult)) {
      return null;
    }

    set({
      activeGame: newGameResult
    })

    return newGameResult;
  },

  spotNewPlates: async (spottedPlates) => {
    const currentGame = get().activeGame;
    if (!currentGame) {
      console.error("No Active Game!");
      return null;
    }

    const updatedGame = await get().apiPost<Game>(`game/${currentGame.gameId}/spotplates`, spottedPlates);

    if (isApiError(updatedGame)) {
      return null;
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
      return null;
    }
    
    const endedGameResult = await get().apiPost<Game>(`game/${currentGame.gameId}/endgame`);
    
    if (isApiError(endedGameResult)) {
      return null;
    }

    set({
      activeGame: null
    });

    return;
  },

  retrieveGameHistory: async () => {
    const gameHistory = await get().apiGet<GameHistory>("game/history");
    if (isApiError(gameHistory)) {
      return null;
    }

    set({
      gameHistory: gameHistory
    });

    return gameHistory;
  }
});

