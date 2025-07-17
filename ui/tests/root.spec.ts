import test, { expect } from "@playwright/test";
import { CreateBaseAppWithInitData, CreateEmptyBaseApp, createNewGame, mockSingleApiRequest } from "./test-helpers";
import type { GameHistory } from "~/appState/GameSlice";

test("has title", async ({ context }) => {
  const page = await CreateEmptyBaseApp(context);

  await page.goto("/");

  // Expect a title "to contain" a substring.
  await expect(page).toHaveTitle(/The License Plate Game/);
});

test("redirect to history page on no active game", async ({ context }) => {
  await mockSingleApiRequest(context,
      "*/**/api/game/history",
      "GET",
      { json: <GameHistory>{
        numberOfGames: 0,
        spotStats: {}
      }}
    );

  const page = await CreateEmptyBaseApp(context);
  
  await page.goto("/");

  await expect(page).toHaveURL("/history");
});

test("redirect to game page on active game", async ({ context }) => {
  const page = await CreateBaseAppWithInitData(context,
    null,
    null,
    createNewGame()
  );
  
  await page.goto("/");

  await expect(page).toHaveURL("/game");
});