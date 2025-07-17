import type { StateCreator } from "zustand";
import type { Game } from "~/game-core/models/Game";
import type { LicensePlateSpot } from "~/game-core/models/LicensePlateSpot";
import type { AppStore } from "./AppStore";
import type { PlayerInfo } from "~/appState/UserAccount";
import { isApiError } from "~/appState/apiError";

export interface GameHistory {
  numberOfGames: number,
  spotStats: { [key: string]: number }
}

export interface GameSlice {
  activeGame: Game | null;

  retrievePlayerData: () => Promise<boolean>;
  startNewGame: (name: string) => Promise<Game | null>;
  spotNewPlates: (spottedPlates: LicensePlateSpot[]) => Promise<Game | null>;
  finishCurrentGame: () => Promise<void | null>;
  retrieveGameHistory: () => Promise<GameHistory | null>;
}

export interface PlayerData {
  player: PlayerInfo | null,
  activeGames: Game[]
}

export const createGameSlice: StateCreator<AppStore, [], [], GameSlice> = (set, get) => ({
  activeGame: null,

  retrievePlayerData: async () => {
    const apiGet = get().get;

    const playerData = await apiGet<PlayerData>("user/userData")

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
  
  startNewGame: async (name: string) => {
    if (get().activeGame) {
      console.error("Only one active game is allowed!");
      return null;
    }

    const newGameRequest = {
      newGameName: name
    };

    const newGameResult = await get().post<Game>("game", newGameRequest);

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

    const updatedGame = await get().post<Game>(`game/${currentGame.gameId}/spotplates`, spottedPlates);

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
    
    const endedGameResult = await get().post<Game>(`game/${currentGame.gameId}/endgame`);
    
    if (isApiError(endedGameResult)) {
      return null;
    }

    set({
      activeGame: null
    });

    return;
  },

  retrieveGameHistory: async () => {
    const gameHistory = await get().get<GameHistory>("game/history");
    if (isApiError(gameHistory)) {
      return null;
    }

    return gameHistory;
  }
});

