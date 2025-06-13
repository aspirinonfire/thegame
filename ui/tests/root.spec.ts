import { test, expect } from '@playwright/test';
import { createEmptyAppState, createNewGame, SetAppState } from './test-helpers';

test('has title', async ({ page }) => {
  await page.goto('/');

  // Expect a title "to contain" a substring.
  await expect(page).toHaveTitle(/The License Plate Game/);
});

test('redirect to history page on very first navigation', async ({ page }) => {
  await page.goto('/');

  await expect(page).toHaveURL("/history");
});

test('redirect to history page on no active game', async ({ browser }) => {
  const context = await browser.newContext();

  const stateWithNoActiveGame = createEmptyAppState();

  await SetAppState(context, stateWithNoActiveGame);

  const page = await context.newPage();
  
  await page.goto('/');

  await expect(page).toHaveURL("/history");

  await context.close();
});

test('redirect to game page on active game', async ({ browser }) => {
  const context = await browser.newContext();

  const stateWithActiveGame = createEmptyAppState();
  stateWithActiveGame.state.activeGame = createNewGame();

  await SetAppState(context, stateWithActiveGame);
  
  const page = await context.newPage();
  
  await page.goto('/');

  await expect(page).toHaveURL("/game");

  await context.close();
});