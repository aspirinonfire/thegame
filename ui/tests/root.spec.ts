import { test, expect } from "@playwright/test";
import { createEmptyAppState, createNewGame, SetAppState } from "./test-helpers";

test("has title", async ({ page }) => {
  await page.goto("/");

  // Expect a title "to contain" a substring.
  await expect(page).toHaveTitle(/The License Plate Game/);
});

test("redirect to history page on very first navigation", async ({ page }) => {
  await page.goto("/");

  await expect(page).toHaveURL("/history");
});

test("redirect to history page on no active game", async ({ browser }) => {
  const context = await browser.newContext();

  const stateWithNoActiveGame = createEmptyAppState();

  await SetAppState(context, stateWithNoActiveGame);

  const page = await context.newPage();
  
  await page.goto("/");

  await expect(page).toHaveURL("/history");

  await context.close();
});

test("redirect to game page on active game", async ({ browser }) => {
  const context = await browser.newContext();

  const stateWithActiveGame = createEmptyAppState();
  stateWithActiveGame.state.activeGame = createNewGame();

  await SetAppState(context, stateWithActiveGame);
  
  const page = await context.newPage();
  
  await page.goto("/");

  await expect(page).toHaveURL("/game");

  await context.close();
});

test("will migrate old nextjs data and show correct score for active game", async ({browser}) => {
  const context = await browser.newContext();

  context.addInitScript(() => {
    console.log("adding old nexjs data to localstorage");
    localStorage.setItem("currentGame", `{"dateCreated":"2024-06-22T02:13:21.864Z","createdBy":"Alex","id":"1719022401864","licensePlates":{"US-AR":{"country":"US","stateOrProvince":"AR","dateSpotted":"2024-06-22T02:13:24.877Z","spottedBy":"Alex"},"US-CA":{"country":"US","stateOrProvince":"CA","dateSpotted":"2024-06-22T02:13:25.791Z","spottedBy":"Alex"},"US-CO":{"country":"US","stateOrProvince":"CO","dateSpotted":"2024-06-22T02:13:26.201Z","spottedBy":"Alex"},"US-AZ":{"country":"US","stateOrProvince":"AZ","dateSpotted":"2024-06-22T03:58:41.901Z","spottedBy":"Alex"},"US-MN":{"country":"US","stateOrProvince":"MN","dateSpotted":"2024-06-22T03:58:43.645Z","spottedBy":"Alex"},"US-MI":{"country":"US","stateOrProvince":"MI","dateSpotted":"2024-06-22T03:58:43.968Z","spottedBy":"Alex"},"CA-BC":{"country":"CA","stateOrProvince":"BC","dateSpotted":"2024-06-24T17:58:20.586Z","spottedBy":"Alex"}},"name":"2024-06-22T02:13:21.661Z","score":{"totalScore":11,"milestones":[]}}`);
    localStorage.setItem("pastGames", `[{"dateCreated":"2024-06-21T20:40:57.108Z","createdBy":"Alex","id":"1719002457108","licensePlates":{"US-AL":{"dateSpotted":"2024-06-21T20:41:03.631Z","spottedBy":"Alex","stateOrProvince":"AL","country":"US"},"US-AK":{"dateSpotted":"2024-06-21T20:41:03.631Z","spottedBy":"Alex","stateOrProvince":"AK","country":"US"},"US-AZ":{"dateSpotted":"2024-06-21T20:41:03.631Z","spottedBy":"Alex","stateOrProvince":"AZ","country":"US"},"US-AR":{"dateSpotted":"2024-06-21T20:41:03.631Z","spottedBy":"Alex","stateOrProvince":"AR","country":"US"}},"name":"Test game","score":{"totalScore":8,"milestones":[]},"dateFinished":"2024-06-21T20:41:03.631Z"},{"dateCreated":"2024-06-21T20:41:13.623Z","createdBy":"Alex","id":"1719002473623","licensePlates":{"US-AR":{"dateSpotted":"2024-06-21T20:41:20.549Z","spottedBy":"Alex","stateOrProvince":"AR","country":"US"},"US-CA":{"dateSpotted":"2024-06-21T20:41:20.549Z","spottedBy":"Alex","stateOrProvince":"CA","country":"US"},"US-AZ":{"country":"US","stateOrProvince":"AZ","dateSpotted":"2024-06-22T00:11:14.857Z","spottedBy":"Alex"}},"name":"Test game","score":{"totalScore":5,"milestones":[]},"dateFinished":"2024-06-22T00:11:14.857Z"}]`);
  });

  const page = await context.newPage();
  
  await page.goto("/");

  await expect(page).toHaveURL("/game");

  const scoreElement = page.getByTestId("current-score");
  await expect(scoreElement).toHaveText("Score: 11");

  await context.close();
});

test("will migrate old nextjs data and show correct history", async ({browser}) => {
  const context = await browser.newContext();

  context.addInitScript(() => {
    console.log("adding old nexjs data to localstorage");
    localStorage.setItem("currentGame", `{"dateCreated":"2024-06-22T02:13:21.864Z","createdBy":"Alex","id":"1719022401864","licensePlates":{"US-AR":{"country":"US","stateOrProvince":"AR","dateSpotted":"2024-06-22T02:13:24.877Z","spottedBy":"Alex"},"US-CA":{"country":"US","stateOrProvince":"CA","dateSpotted":"2024-06-22T02:13:25.791Z","spottedBy":"Alex"},"US-CO":{"country":"US","stateOrProvince":"CO","dateSpotted":"2024-06-22T02:13:26.201Z","spottedBy":"Alex"},"US-AZ":{"country":"US","stateOrProvince":"AZ","dateSpotted":"2024-06-22T03:58:41.901Z","spottedBy":"Alex"},"US-MN":{"country":"US","stateOrProvince":"MN","dateSpotted":"2024-06-22T03:58:43.645Z","spottedBy":"Alex"},"US-MI":{"country":"US","stateOrProvince":"MI","dateSpotted":"2024-06-22T03:58:43.968Z","spottedBy":"Alex"},"CA-BC":{"country":"CA","stateOrProvince":"BC","dateSpotted":"2024-06-24T17:58:20.586Z","spottedBy":"Alex"}},"name":"2024-06-22T02:13:21.661Z","score":{"totalScore":11,"milestones":[]}}`);
    localStorage.setItem("pastGames", `[{"dateCreated":"2024-06-21T20:40:57.108Z","createdBy":"Alex","id":"1719002457108","licensePlates":{"US-AL":{"dateSpotted":"2024-06-21T20:41:03.631Z","spottedBy":"Alex","stateOrProvince":"AL","country":"US"},"US-AK":{"dateSpotted":"2024-06-21T20:41:03.631Z","spottedBy":"Alex","stateOrProvince":"AK","country":"US"},"US-AZ":{"dateSpotted":"2024-06-21T20:41:03.631Z","spottedBy":"Alex","stateOrProvince":"AZ","country":"US"},"US-AR":{"dateSpotted":"2024-06-21T20:41:03.631Z","spottedBy":"Alex","stateOrProvince":"AR","country":"US"}},"name":"Test game","score":{"totalScore":8,"milestones":[]},"dateFinished":"2024-06-21T20:41:03.631Z"},{"dateCreated":"2024-06-21T20:41:13.623Z","createdBy":"Alex","id":"1719002473623","licensePlates":{"US-AR":{"dateSpotted":"2024-06-21T20:41:20.549Z","spottedBy":"Alex","stateOrProvince":"AR","country":"US"},"US-CA":{"dateSpotted":"2024-06-21T20:41:20.549Z","spottedBy":"Alex","stateOrProvince":"CA","country":"US"},"US-AZ":{"country":"US","stateOrProvince":"AZ","dateSpotted":"2024-06-22T00:11:14.857Z","spottedBy":"Alex"}},"name":"Test game","score":{"totalScore":5,"milestones":[]},"dateFinished":"2024-06-22T00:11:14.857Z"}]`);
  });

  const page = await context.newPage();
  
  await page.goto("/history");

  const scoreElement = page.getByTestId("total-games-played");
  await expect(scoreElement).toHaveText("Total Games Played: 2");

  await context.close();
});