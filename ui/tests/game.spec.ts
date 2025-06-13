import { test, expect } from '@playwright/test';
import { createEmptyAppState, createNewGame, SetAppState } from './test-helpers';

test('will start new game', async ({ page }) => {
  await page.goto('/game');

  const startNewGameBtn = page.getByTestId('start-new-game');

  await startNewGameBtn.click();

  const mapElement = page.getByTestId('game-map-activeGame');

  expect(mapElement).toBeVisible();
});

test('will update map and score on spotting California', async ({ browser }) => {
  const context = await browser.newContext();

  const stateWithActiveGame = createEmptyAppState();
  stateWithActiveGame.state.activeGame = createNewGame();

  await SetAppState(context, stateWithActiveGame);
  
  const page = await context.newPage();
  
  await page.goto('/game');

  // click on map
  const mapElement = page.getByTestId('game-map-activeGame');
  expect(mapElement).toBeVisible();
  await mapElement.click();

  // select US-CA plate and save selection
  const usCaPlateDiv = page.getByTestId('select-plate-US-CA');
  expect(usCaPlateDiv).toBeVisible();
  await usCaPlateDiv.click();

  const saveChangesBtn = page.getByTestId('save-spotted-changes');
  expect(saveChangesBtn).toBeVisible();
  await saveChangesBtn.click();

  // assert california is defined
  const usCaMap = page.getByTestId('map-us-ca');
  expect(usCaMap).toBeVisible();
  await expect(usCaMap).toContainClass('fill-amber-700');

  // assert other states (such as NV) are not selected
  const usOrMap = page.getByTestId('map-us-or');
  expect(usOrMap).toBeVisible();
  await expect(usOrMap).not.toContainClass('fill-amber-700');

  // assert score
  const scoreElement = page.getByTestId('current-score');
  await expect(scoreElement).toHaveText('Score: 1');
});