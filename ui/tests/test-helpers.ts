import type { BrowserContext } from "@playwright/test";
import type { Game } from "~/game-core/models/Game";

export function createEmptyAppState() {
  return {
    version: 0,
    state: {
      activeGame: <Game | null>null,
      pastGames: <Game[]>[],
      isMigratedFromNextJs: true
    }
  }
};

export function createNewGame() : Game {
  return {
    createdBy: "Test",
    dateCreated: new Date(),
    id: new Date().getTime().toString(),
    name: "test",
    licensePlates: [],
    score: {
      totalScore: 0,
      milestones: []
    }
  }
}

export async function SetAppState(context: BrowserContext, state: any) {
  const stateAsJson = JSON.stringify(state, null, 2);
  console.log('Setting app state...\n', stateAsJson);
  await context.addInitScript(state => localStorage.setItem("Game UI", state),
    stateAsJson);
};