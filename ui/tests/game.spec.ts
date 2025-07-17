import { test, expect } from "@playwright/test";
import { CreateBaseAppWithInitData, CreateEmptyBaseApp, createNewGame, mockSingleApiRequest, testGameId } from "./test-helpers";

test("will start new game", async ({ context }) => {
  await mockSingleApiRequest(context,
    "*/**/api/game",
    "POST",
    { json: createNewGame() }
  );

  const page = await CreateEmptyBaseApp(context);

  await page.goto("/game");

  const startNewGameBtn = page.getByTestId("start-new-game");

  await startNewGameBtn.click();

  const mapElement = page.getByTestId("game-map-activeGame");

  await expect(mapElement).toBeVisible();
});

test("will update map, score, and milestones on spotting West Coast", async ({ context }) => {
  const updatedGame = createNewGame();
  updatedGame.spottedPlates = [
    {
      key: "US-CA",
      country: "US",
      stateOrProvince: "CA",
      spottedByPlayerId: 1,
      spottedByPlayerName: "Test Player",
      spottedOn: new Date()
    },
    {
      key: "US-OR",
      country: "US",
      stateOrProvince: "OR",
      spottedByPlayerId: 1,
      spottedByPlayerName: "Test Player",
      spottedOn: new Date()
    },
    {
      key: "US-WA",
      country: "US",
      stateOrProvince: "WA",
      spottedByPlayerId: 1,
      spottedByPlayerName: "Test Player",
      spottedOn: new Date()
    }
  ];
  updatedGame.score = {
    totalScore: 10,
    achievements: [ "West Coast" ]
  };

  await mockSingleApiRequest(context,
    `*/**/api/game/${testGameId}/spotplates`,
    "POST",
    { json : updatedGame });

  const page = await CreateBaseAppWithInitData(context,
    null,
    null,
    createNewGame());

  await page.goto("/game");

  // click on map
  const mapElement = page.getByTestId("game-map-activeGame");
  await expect(mapElement).toBeVisible();
  await mapElement.click();

  // Here user selects US-CA, US-OR, and US-WA plate and then saves selection
  // This interaction is mocked by a specific response we send back

  const saveChangesBtn = page.getByTestId("save-spotted-changes");
  await expect(saveChangesBtn).toBeVisible();
  await saveChangesBtn.click();

  // assert US-CA is selected
  const usCaMap = page.getByTestId("map-us-ca");
  await expect(usCaMap).toBeVisible();
  await expect(usCaMap).toContainClass("fill-amber-700");

  // assert US-OR is selected
  const orCaMap = page.getByTestId("map-us-or");
  await expect(orCaMap).toBeVisible();
  await expect(orCaMap).toContainClass("fill-amber-700");

  // assert US-WA is selected
  const waCaMap = page.getByTestId("map-us-wa");
  await expect(waCaMap).toBeVisible();
  await expect(waCaMap).toContainClass("fill-amber-700");

  // assert US-NV is NOT selected
  const usNvMap = page.getByTestId("map-us-nv");
  await expect(usNvMap).toBeVisible();
  await expect(usNvMap).not.toContainClass("fill-amber-700");

  // assert score
  const scoreElement = page.getByTestId("current-score");
  await expect(scoreElement).toHaveText("Score: 10");

  // assert milestone
  const milestones = page.getByTestId("current-milestones");
  expect(await milestones.allTextContents()).toContain("West Coast");
});