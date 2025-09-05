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

### Frontend followup - 2
1. Do not inject test-specific stuff into non-test code! `(globalThis as any).__TEST_AI_RESULTS` is not something I ever want to see in the non-test code ever. If you need to set form data - use init scripts, or manually trigger clicks.
2. Split your backend tests into specific cases they cover - invalid char removal, truncation must be two different tests.
3. I do not believe your backend sanitization works correctly. Also, your tests are not good - for starters, use `Assert.Equal(expected, actual)` and not `Containts` in the unit test. I want to see the exact string that is expected to be produced by the test. For truncation - use text length. If you find an error in sanitization code - fix the logic with regex.

### Frontend followup - 3
1. When asserting, do not pre/post process expected value. Use hardcoded string literal (eg. do not use trim() or such)
2. Regex you are using has issues - it should describe what is allowed, not what is forbidden. Assume normal english language characters, numbers, and punctuations. It also needs execution timeout. Lastly, just move NormalizePrompt as static under SpotLicensePlatesCommandHandler instead of a dedicated internal static partial class (I assume this will also remove the necessity of modifying API csproj)
3. When interacting with page elements during ui testing, you must use testid. Modify html structure as needed.

### Frontend followup - 4
1. By allowlist I meant remove everything that is not typical english alphabet, digits, or punctuation.
2. Do not add anything to the window for ui testing. I see what you did with plateDescriptionClassifier but instead of returning __AI_MAP, just return hardcoded object. Use multi-line string for readability. 

### Frontend followup - 5
1. Lets change regex to something allong the lines of `[a-zA-Z....`

### Frontend followup - 6
1. I do not want to see \x20... ranges in regex. Let's simplify it to capture letters, digits, spaces, dashes, underscores, commas, dots, question marks, exclamation marks, quotes, and apostrophies

---
### Frontend platepicker cleanup initial prompt
I want to cleanup `platepicker.tsx`. I see it is bit difficult to follow. Primarily, I want to use `TerritoriyToRender` interface to also capture whether or not a particular plate was selected. Parent class passes in plateData which an array of selected LicensePlateSpots. I also have allTerritoriesToRender which is a collection of all possible plates that can be rendered and it contains form-specific details. I want to add `isSelected: bool` property. During component initialization, I want to build `allTerritoriesToRender`, and set the initial values for the new property. During user interactions, I want to flip this flag one way or the other instead of doing misc lookups. Once user saves the form by invoking `handelSaveNewSpots`, I want to parse this collection and build a correct updatedForm. I expect `handleCheckboxChange` to be more lean. Use lookups as needed for clarity (eg I prefer `lookup[key]` vs `someArray.filter(...)` for readability)

### Frontend platepicker cleanup followup - 1
Let'd dial it in further. I want to maintain a single `platesToRender` collection of type `TerritoriyToRender`. In addition to `isSelected`, I want to add and maintain `isVisible: boolean` flag, with default value of `true` - or show all once modal is shown.
1. When in non-AI search mode, `isVisible` should match the search term. Instead of array filter, just set the flag value based on the existing logic. Order alphabetically.
2. When in AI search mode, set isVisible in the way that will produce the following - ordered by highest search probability at the top, with probability greater than 0.03 or 3%.
3. When not in any search mode - show all in alphabetical order (default configuratio).
4. When saving the form - save all selected values.
5. I want to maintain ml prompt value for each plate to render in the same collection rather than separate lookup. For simplicity, reset aiPrompt value only when unchecking/deselecting in any mode other than AI (quick search, or no search at all). If another ai search is triggered and plate selection did not change, do not touch the value.

### Frontend platepicker cleanup followup - 2
Lets simplify this file further.
1. Remove `CurrentAiPrompt` you added. We don't need to track it.
2. We don't need both territoriesToRender and platesToRender. Pick one collection and work with it.
3. When handling checkbox changes. If plate gets selected and is aiMode, set `aiPrompt` value on the plate to the searchTerm. Thats it. If deselecting, remove promptValue from the render plate.
4. Remove any changes that are not supporting this logic. Remove any other dead code.

### Frontend platepicker cleanup followup - 2
You changed code I did not ask you to.
1. I want search helper functions to be pure - return new set of plates to render. Call setPlatesToRender in useEffect not in these calls. 
2. Revert the logic that decides whether ai search mode is used. If quick search returns empty - go to AI. Which means you need to bring back getQuickSearchResults. As a reminder - you need to return a full list with flags set correctly.
3. Finally `return t.aiPrompt ? ({ ...spot, mlPrompt: t.aiPrompt } as any) : spot;` is not needed. You should always set aiPrompt on save. Use case - user uses AI search to pick one plate, and the quick search to pick another. Second action will set aimode to false, I do not want to lose prompt in this scenario because first plate is still selected with the help of ai search.

### Frontend platepicker cleanup followup - 3
You did not follow my instructions. 
1. Bring back `getQuickSearchResults` and and how it is being consumed in `useEffect`
2. `getPlatesMatchingAiSearch` should return a collection of new plates to render only. Do not call `setPlatesToRender`
3. Why are you calling setPlatesToRender inside handleCheckboxChange?? You should have a reference to a territory from the plates to render collection. You just need to set appropriate props, no?
4. You have completely my comment around `        return { ...spot, mlPrompt: t.aiPrompt ?? null } as LicensePlateSpot; ` when instantiating LicensePlateSpot, set aiPrompt right there, whithout any ifs. Also, why double type definition? spot is already declared as LicensePlateSpot, you do not need `as LicensePlateSpot` after. As a matter of fact, you don't even need a temp variable, you can just return mapped/instantiated LicensePlateSpot directly in lambda without an explicit return statement.

### Frontend platepicker cleanup followup - 4
You did not complete what I've asked and changed lines i did not ask you to. I changed your reasoning mode to high, try again.