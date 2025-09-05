import { test, expect } from "@playwright/test";
import { CreateBaseAppWithInitData, createNewGame, mockSingleApiRequest, testGameId } from "./test-helpers";

test.describe("AI prompt spot saving", () => {
  test("includes mlPrompt only for plates selected via AI search", async ({ context }) => {
    const updatedGame = createNewGame();
    updatedGame.spottedPlates = [];

    let capturedBody: any = null;
    await context.route(`*/**/api/game/${testGameId}/spotplates`, async route => {
      capturedBody = route.request().postDataJSON();
      await route.fulfill({ json: updatedGame });
    });

    // Stub classifier module via routing and use init script to provide per-prompt results
    await context.route(/.*plateDescriptionClassifier.*$/, async route => {
      const body = `
export class OnnxPlateDescriptionClassifier {
  async init() {}
  async predictAll(query) {
    if (query === 'blue sedan near ocean') {
      return [
        { label: 'US-CA', probability: 0.9 },
        { label: 'US-OR', probability: 0.8 }
      ];
    }
    return [];
  }
}
`;
      await route.fulfill({ contentType: 'application/javascript', body });
    });

    
    const page = await CreateBaseAppWithInitData(context, null, null, createNewGame());

    await page.goto("/game");

    // Open picker
    await page.getByTestId("game-map-activeGame").click();

    // Type an AI prompt; debounce is 1s, so wait a bit
    const prompt = "blue sedan near ocean";
    await page.getByTestId("plate-search-input").fill(prompt);
    await page.waitForTimeout(1200);

    // Select two AI results
    await page.getByTestId("select-plate-US-CA").click();
    await page.getByTestId("select-plate-US-OR").click();

    // Clear search so a quick search selection will not carry prompt
    await page.getByTestId('plate-search-clear').click();

    // Select one via quick list (e.g., WA is visible)
    await page.getByTestId("select-plate-US-WA").click();

    await page.getByTestId("save-spotted-changes").click();

    expect(Array.isArray(capturedBody)).toBeTruthy();
    const body = capturedBody as any[];
    const ca = body.find(p => p.key === 'US-CA');
    const or = body.find(p => p.key === 'US-OR');
    const wa = body.find(p => p.key === 'US-WA');
    expect(ca.mlPrompt).toBe(prompt);
    expect(or.mlPrompt).toBe(prompt);
    expect(wa.mlPrompt).toBeNull();
  });

  test("supports multiple prompts for different AI selections", async ({ context }) => {
    const updatedGame = createNewGame();
    updatedGame.spottedPlates = [];

    let capturedBody: any = null;
    await context.route(`*/**/api/game/${testGameId}/spotplates`, async route => {
      capturedBody = route.request().postDataJSON();
      await route.fulfill({ json: updatedGame });
    });

    // Stub classifier; return hardcoded predictions for two prompts
    await context.route(/.*plateDescriptionClassifier.*$/, async route => {
      const body = `
export class OnnxPlateDescriptionClassifier {
  async init() {}
  async predictAll(query) {
    if (query === 'beach scene') {
      return [ { label: 'US-CA', probability: 0.9 } ];
    }
    if (query === 'forest road') {
      return [ { label: 'US-OR', probability: 0.95 } ];
    }
    return [];
  }
}
`;
      await route.fulfill({ contentType: 'application/javascript', body });
    });
    
    const page = await CreateBaseAppWithInitData(context, null, null, createNewGame());
    await page.goto("/game");
    await page.getByTestId("game-map-activeGame").click();
    const promptA = "beach scene";
    await page.getByTestId("plate-search-input").fill(promptA);
    await page.waitForTimeout(1200);
    await page.getByTestId("select-plate-US-CA").click();

    // Second AI prompt -> OR
    await page.getByTestId("plate-search-input").fill("");
    await page.waitForTimeout(200);
    // No window pollution; classifier stub handles responses based on query
    const promptB = "forest road";
    await page.getByTestId("plate-search-input").fill(promptB);
    await page.waitForTimeout(1200);
    await page.getByTestId("select-plate-US-OR").click();

    await page.getByTestId("save-spotted-changes").click();

    const body = capturedBody as any[];
    const ca = body.find(p => p.key === 'US-CA');
    const or = body.find(p => p.key === 'US-OR');
    expect(ca.mlPrompt).toBe(promptA);
    expect(or.mlPrompt).toBe(promptB);
  });
});
