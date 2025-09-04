# CODEX Agent prompts

## 2025-09-04
### Backend initial prompt
I want to update license plate spotting feature in the api to include optional ML prompt associated with the spot. Store this information in a separate table. When I will be writing queries against this table, I will need game id, player id who created this record, license plate info (see gamelicenseplate for an example), and date this record was created. Your test coverage should cover the following scenarions:
1. Create spot with ml prompt - assert it is there
2. Create spot without ml prompt - assert operation did not fail
3. Create spot with ml prompt, then remove spot (you can just issue another operation where spot is no longer set. See corresponding invariant in the 1. aggregate root with the logic of removing the spot).

### Backend followup - 1
1. ML Prompt is not part of the domain so I do not want to modify aggregate root. Just modify the command handler.
2. I want to ensure all foreign keys are valid. I do not see rela definitions or constraints. Make sure you avoid modifying existing entities or configs. I will query directly from this entity. Eg .Set<>.Where(prompt => prompt.Game.GameId == 123).
3. I want no more than one migration for this commit. If you need to modify the existing, make sure the commit will only have one migration.

### Backend followup - 2
For testing you are re-using private util methods from the other test. Promote these to TestUtils and refactor existing as needed.

---
### Frontend initial prompt
I want to update UI code to send in the ml prompt when applicable to the backend. You just completed the endpoint update, and now I want you to wire up the frontend. See `platepicker.tsx`. Here's the logic I want you to integrate:
1. When user uses search feature in a way that triggers AI search, I want you to store this prompt along with any selected license plates.
2. When user is not using ai search, do not send any prompts, even if previously was used.
3. Consider scenario where user uses AI search to select multiple plates, so there will be n-sets of checked license plates with different prompts.
4. This is a free-form user input so consider XSS, SQL-injection, and other possible vectors of possible. Ensure backend can handle these properly. Right now these prompts will not be displayed in the UI but they might in the future (outside of the scope of this prompt).
5. We'll handle user notification around ai search training in the follow-up work so assume no additional checks to enable this feature are required.

Few important notes:
1. To run this app end-to-end, you will need to run Aspire. All configs are in good shape.
2. I expect most of this work to be done in the UI. Backend changes should not exceed the scope of step 4 above.


### Frontend followup - 1
1. Sanitize in the backend only.
2. Include unit tests for backend sanitization. Cases to cover: input that does not need sanitization, input that needs sanitization.
3. See TerritoryToRender and piggyback off this class. It already carries ML-specific data (searchProbability). 
4. Add playwright tests to cover FE behavior around saving. I want to ensure your logic works correctly for the scenarios outlined above.

### Frontend follow - 2
1. Do not inject test-specific stuff into non-test code! `(globalThis as any).__TEST_AI_RESULTS` is not something I ever want to see in the non-test code ever. If you need to set form data - use init scripts, or manually trigger clicks.
2. Split your backend tests into specific cases they cover - invalid char removal, truncation must be two different tests.
3. I do not believe your backend sanitization works correctly. Also, your tests are not good - for starters, use `Assert.Equal(expected, actual)` and not `Containts` in the unit test. I want to see the exact string that is expected to be produced by the test. For truncation - use text length. If you find an error in sanitization code - fix the logic with regex.

### Frontend follow - 3
1. When asserting, do not pre/post process expected value. Use hardcoded string literal (eg. do not use trim() or such)
2. Regex you are using has issues - it should describe what is allowed, not what is forbidden. Assume normal english language characters, numbers, and punctuations. It also needs execution timeout. Lastly, just move NormalizePrompt as static under SpotLicensePlatesCommandHandler instead of a dedicated internal static partial class (I assume this will also remove the necessity of modifying API csproj)
3. When interacting with page elements during ui testing, you must use testid. Modify html structure as needed.

### Frontend follow - 4
1. By allowlist I meant remove everything that is not typical english alphabet, digits, or punctuation.
2. Do not add anything to the window for ui testing. I see what you did with plateDescriptionClassifier but instead of returning __AI_MAP, just return hardcoded object. Use multi-line string for readability. 

### Frontend follow - 5
1. Lets change regex to something allong the lines of `[a-zA-Z....`

### Frontend follow - 6
1. I do not want to see \x20... ranges in regex. Let's simplify it to capture letters, digits, spaces, dashes, underscores, commas, dots, question marks, exclamation marks, quotes, and apostrophies