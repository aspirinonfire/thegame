import type { BrowserContext, Page, Route } from "@playwright/test";
import type { PlayerData } from "~/appState/GameSlice";
import type { PlayerInfo } from "~/appState/UserAccount";
import type { Game } from "~/game-core/models/Game";

export function createEmptyAppState() {
  return {
    version: 0,
    state: {
      activeGame: <Game | null>null,
      apiAccessToken: "test-token"
    }
  }
};

export const testGameId = 1000;
export function createNewGame() : Game {
  return {
    createdByPlayerId: 123,
    createdByPlayerName: "Test",
    dateCreated: new Date("2025-07-17"),
    gameId: testGameId,
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

export async function mockSingleApiRequest(
  contextOrPage: BrowserContext | Page,
  urlOrRegex: string | RegExp,
  method: "GET" | "POST" | "PUT" | "PATH" | "DELETE",
  response: Parameters<Route['fulfill']>[0]
) {
    contextOrPage
      .route(
        urlOrRegex,
        async route => {
          const requestMethod = route.request().method();
          if (requestMethod.toLowerCase() == method.toLocaleLowerCase()) {
            console.log(`Fullfilling ${requestMethod} ${route.request().url()} by ${method} ${urlOrRegex} with mocked data...`);
            await route.fulfill(response);
            return;
          }
          
          console.log(`${requestMethod} ${route.request().url()} is not matching ${method} ${urlOrRegex}. Falling back to next handler...`);
          await route.fallback();
        },
        { times: 1});
  }

export async function CreateEmptyBaseApp(context: BrowserContext)
{
  return CreateBaseAppWithInitData(context,
    null,
    null,
    null
  );
}

export async function CreateBaseAppWithInitData(context: BrowserContext,
  state: any | null,
  initPlayer: PlayerInfo | null,
  initGame: Game | null
) {
  const stateToSet = state ?? createEmptyAppState();
  await SetAppState(context, stateToSet);

  const page = await context.newPage();

  const testUser: PlayerInfo = initPlayer ?? {
    playerId: 1,
    playerName: "Test Player"
  };

  const gameToInitWith = !!initGame ? [initGame] : [];

  mockSingleApiRequest(page,
    "*/**/api/user/userData",
    "GET",
    { 
      json: <PlayerData>{
        player: testUser,
        activeGames: gameToInitWith
      }
    }
  );

  return page;
}