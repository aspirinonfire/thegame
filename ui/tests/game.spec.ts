import { test, expect } from "@playwright/test";
import { createEmptyAppState, createNewGame, SetAppState } from "./test-helpers";
import { territories } from "~/game-core/gameConfiguration";

test("will start new game", async ({ page }) => {
  await page.goto("/game");

  const startNewGameBtn = page.getByTestId("start-new-game");

  await startNewGameBtn.click();

  const mapElement = page.getByTestId("game-map-activeGame");

  await expect(mapElement).toBeVisible();
});

test("will update map and score on spotting California", async ({ browser }) => {
  const context = await browser.newContext();

  const stateWithActiveGame = createEmptyAppState();
  stateWithActiveGame.state.activeGame = createNewGame();

  await SetAppState(context, stateWithActiveGame);
  
  const page = await context.newPage();
  
  await page.goto("/game");

  // click on map
  const mapElement = page.getByTestId("game-map-activeGame");
  await expect(mapElement).toBeVisible();
  await mapElement.click();

  // select US-CA plate and save selection
  const usCaPlateDiv = page.getByTestId("select-plate-US-CA");
  await expect(usCaPlateDiv).toBeVisible();
  await usCaPlateDiv.click();

  const saveChangesBtn = page.getByTestId("save-spotted-changes");
  await expect(saveChangesBtn).toBeVisible();
  await saveChangesBtn.click();

  // assert california is defined
  const usCaMap = page.getByTestId("map-us-ca");
  await expect(usCaMap).toBeVisible();
  await expect(usCaMap).toContainClass("fill-amber-700");

  // assert other states (such as NV) are not selected
  const usOrMap = page.getByTestId("map-us-or");
  await expect(usOrMap).toBeVisible();
  await expect(usOrMap).not.toContainClass("fill-amber-700");

  // assert score
  const scoreElement = page.getByTestId("current-score");
  await expect(scoreElement).toHaveText("Score: 1");
});

test("will calculate score milestones for West Coast", async ({ browser }) => {
  const context = await browser.newContext();

  const stateWithActiveGame = createEmptyAppState();
  stateWithActiveGame.state.activeGame = createNewGame();
  stateWithActiveGame.state.activeGame.spottedPlates = ["US-CA", "US-OR", "US-WA"]
    .map(key => ({
      key,
      country: key.split("-")[0],
      stateOrProvince: key.split("-")[1],
      spottedByPlayerId: 123,
      spottedByPlayerName: "test",
      spottedOn: new Date()
    }));

  await SetAppState(context, stateWithActiveGame);
  
  const page = await context.newPage();
  
  await page.goto("/game");

  const milestones = page.getByTestId("current-milestones");
  await expect(milestones).toBeHidden();
  expect((await milestones.allTextContents()).length).toBe(0);

  // click on map
  const mapElement = page.getByTestId("game-map-activeGame");
  await expect(mapElement).toBeVisible();
  await mapElement.click();

  // save to re-trigger calculations
  const saveChangesBtn = page.getByTestId("save-spotted-changes");
  await expect(saveChangesBtn).toBeVisible();
  await saveChangesBtn.click();

  await expect(milestones).toBeVisible();
  expect(await milestones.allTextContents()).toContain("West Coast");
});

test("will calculate score milestones for East Coast", async ({ browser }) => {
  const context = await browser.newContext();

  const stateWithActiveGame = createEmptyAppState();
  stateWithActiveGame.state.activeGame = createNewGame();
  stateWithActiveGame.state.activeGame.spottedPlates = ["US-CT",
    "US-DE",
    "US-FL",
    "US-GA",
    "US-ME",
    "US-MD",
    "US-MA",
    "US-NH",
    "US-NJ",
    "US-NY",
    "US-NC",
    "US-RI",
    "US-SC",
    "US-VA"]
    .map(key => ({
      key,
      country: key.split("-")[0],
      stateOrProvince: key.split("-")[1],
      spottedByPlayerId: 123,
      spottedByPlayerName: "test",
      spottedOn: new Date(),
    }));

  await SetAppState(context, stateWithActiveGame);
  
  const page = await context.newPage();
  
  await page.goto("/game");

  const milestones = page.getByTestId("current-milestones");
  await expect(milestones).toBeHidden();
  expect((await milestones.allTextContents()).length).toBe(0);

  // click on map
  const mapElement = page.getByTestId("game-map-activeGame");
  await expect(mapElement).toBeVisible();
  await mapElement.click();

  // save to re-trigger calculations
  const saveChangesBtn = page.getByTestId("save-spotted-changes");
  await expect(saveChangesBtn).toBeVisible();
  await saveChangesBtn.click();

  await expect(milestones).toBeVisible();
  expect(await milestones.allTextContents()).toContain("East Coast");
});

test("will calculate score milestones for Coast to Coast", async ({ browser }) => {
  const context = await browser.newContext();

  const stateWithActiveGame = createEmptyAppState();
  stateWithActiveGame.state.activeGame = createNewGame();
  stateWithActiveGame.state.activeGame.spottedPlates = ["US-CA", "US-AZ", "US-NM", "US-OK", "US-AR", "US-TN", "US-NC"]
    .map(key => ({
      key,
      country: key.split("-")[0],
      stateOrProvince: key.split("-")[1],
      spottedByPlayerId: 123,
      spottedByPlayerName: "test",
      spottedOn: new Date(),
    }));

  await SetAppState(context, stateWithActiveGame);
  
  const page = await context.newPage();
  
  await page.goto("/game");

  const milestones = page.getByTestId("current-milestones");
  await expect(milestones).toBeHidden();
  expect((await milestones.allTextContents()).length).toBe(0);

  // click on map
  const mapElement = page.getByTestId("game-map-activeGame");
  await expect(mapElement).toBeVisible();
  await mapElement.click();

  // save to re-trigger calculations
  const saveChangesBtn = page.getByTestId("save-spotted-changes");
  await expect(saveChangesBtn).toBeVisible();
  await saveChangesBtn.click();

  await expect(milestones).toBeVisible();
  expect(await milestones.allTextContents()).toContain("Coast-to-Coast");
});

test("will calculate score milestones for Coast to Coast and West Coast", async ({ browser }) => {
  const context = await browser.newContext();

  const stateWithActiveGame = createEmptyAppState();
  stateWithActiveGame.state.activeGame = createNewGame();
  stateWithActiveGame.state.activeGame.spottedPlates = ["US-CA", "US-OR", "US-WA", "US-AZ", "US-NM", "US-OK", "US-AR", "US-TN", "US-NC"]
    .map(key => ({
      key,
      country: key.split("-")[0],
      stateOrProvince: key.split("-")[1],
      spottedByPlayerId: 123,
      spottedByPlayerName: "test",
      spottedOn: new Date(),
    }));

  await SetAppState(context, stateWithActiveGame);
  
  const page = await context.newPage();
  
  await page.goto("/game");

  const milestones = page.getByTestId("current-milestones");
  await expect(milestones).toBeHidden();
  expect((await milestones.allTextContents()).length).toBe(0);

  // click on map
  const mapElement = page.getByTestId("game-map-activeGame");
  await expect(mapElement).toBeVisible();
  await mapElement.click();

  // save to re-trigger calculations
  const saveChangesBtn = page.getByTestId("save-spotted-changes");
  await expect(saveChangesBtn).toBeVisible();
  await saveChangesBtn.click();

  await expect(milestones).toBeVisible();
  expect(await milestones.allTextContents()).toContain("West CoastCoast-to-Coast");
});

test("will calculate score milestones when everything is marked", async ({ browser }) => {
  const context = await browser.newContext();

  const stateWithActiveGame = createEmptyAppState();
  stateWithActiveGame.state.activeGame = createNewGame();
  stateWithActiveGame.state.activeGame.spottedPlates = territories
    .map(ter => ({
      key: ter.key,
      country: ter.key.split("-")[0],
      stateOrProvince: ter.key.split("-")[1],
      spottedByPlayerId: 123,
      spottedByPlayerName: "test",
      spottedOn: new Date(),
    }));

  await SetAppState(context, stateWithActiveGame);
  
  const page = await context.newPage();
  
  await page.goto("/game");

  const milestones = page.getByTestId("current-milestones");
  await expect(milestones).toBeHidden();
  expect((await milestones.allTextContents()).length).toBe(0);

  // click on map
  const mapElement = page.getByTestId("game-map-activeGame");
  await expect(mapElement).toBeVisible();
  await mapElement.click();

  // save to re-trigger calculations
  const saveChangesBtn = page.getByTestId("save-spotted-changes");
  await expect(saveChangesBtn).toBeVisible();
  await saveChangesBtn.click();

  await expect(milestones).toBeVisible();
  const allText = await milestones.allTextContents();
  console.log(allText);
  expect(allText).toContain("SouthSouthwestWest CoastEast CoastMidwestCoast-to-CoastGlobetrotter");
});