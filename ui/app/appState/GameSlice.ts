import type { StateCreator } from "zustand";
import CalculateScore from "~/game-core/gameScoreCalculator";
import type { Game } from "~/game-core/models/Game";
import type { LicensePlateSpot } from "~/game-core/models/LicensePlateSpot";
import type { ScoreData } from "~/game-core/models/ScoreData";
import type { AppStore } from "./AppStore";

export interface GameSlice {
  activeGame: Game | null;
  pastGames: Game[];

  startNewGame: (name: string) => Promise<Game | string>;
  spotNewPlates: (spottedPlates: LicensePlateSpot[]) => Promise<Game | string>;
  finishCurrentGame: () => Promise<string | void>;
}

export const createGameSlice: StateCreator<AppStore, [], [], GameSlice> = (set, get) => ({
  activeGame: null,
  pastGames: [],

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

    const updatedGame = <Game>{
      ...currentGame,
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

    // TODO implement!!
    const endedGame = await get().post<Game>(`game/${currentGame.gameId}/endgame`);
    return;

    //// use last spot as date finished
    // const lastSpot = currentGame.spottedPlates
    //   .map(plate => plate.spottedOn)
    //   .filter(date => !!date)
    //   .sort()
    //   .at(-1);

    // if (!!lastSpot) {
    //   currentGame.endedOn = lastSpot;

    //   const pastGames = get().pastGames;
    //   pastGames.push(currentGame);

    //   set({
    //     pastGames: pastGames,
    //   });
    // }

    // set({
    //   activeGame: null
    // });
  },
});

