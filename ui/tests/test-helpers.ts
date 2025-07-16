import type { BrowserContext } from "@playwright/test";
import type { GameHistory } from "~/appState/GameSlice";
import type { Game } from "~/game-core/models/Game";

export function createEmptyAppState() {
  return {
    version: 0,
    state: {
      activeGame: <Game | null>null
    }
  }
};

export function createNewGame() : Game {
  return {
    createdByPlayerId: 123,
    createdByPlayerName: "Test",
    dateCreated: new Date(),
    gameId: new Date().getTime(),
    gameName: "test",
    spottedPlates: [],
    score: {
      totalScore: 0,
      achievements: []
    }
  }
}

export async function SetAppState(context: BrowserContext, state: any) {
  const stateAsJson = JSON.stringify(state, null, 2);
  console.log('Setting app state...\n', stateAsJson);
  await context.addInitScript(state => localStorage.setItem("Game UI", state),
    stateAsJson);
};