import { UsStateBorders, mockGameData } from "../data";
import { Game, LicensePlate, ScoreData } from "./game";
import { StateBorder } from "./gameDataTerritory";

const westCoastUsStatesLkp: Set<string> = mockGameData
  .filter(state => !!state.modifier && state.modifier.indexOf('West Coast') >= 0)
  .map(state => state.shortName)
  .reduce((lkp, state) => lkp.add(state), new Set<string>());

const eastCoastStatesLkp: Set<string> = mockGameData
  .filter(state => !!state.modifier && state.modifier?.indexOf('East Coast') >= 0)
  .map(state => state.shortName)
  .reduce((lkp, state) => lkp.add(state), new Set<string>());

const scopeMultiplierByPlateLkp: Map<string, number> = mockGameData
  .reduce((lkp, territory) => {
    lkp.set(`${territory.country}-${territory.shortName}`, territory.scoreMultiplier ?? 1);
    return lkp;
  }, new Map<string, number>());

function hasWestCoast(plates: { [key: string]: LicensePlate }): boolean {
  const markedUsStates = Object.keys(plates)
    .map(key => plates[key])
    .filter(plate => plate.country == 'US')
    .map(plate => plate.stateOrProvince);

  return markedUsStates
    .filter(state => westCoastUsStatesLkp.has(state))
    .length == westCoastUsStatesLkp.size;
}

function hasEastCoast(plates: { [key: string]: LicensePlate }): boolean {
  const markedUsStates = Object.keys(plates)
    .map(key => plates[key])
    .filter(plate => plate.country == 'US')
    .map(plate => plate.stateOrProvince);

  return markedUsStates
    .filter(state => eastCoastStatesLkp.has(state))
    .length == eastCoastStatesLkp.size;
}

function hasTransAtlantic(plates: { [key: string]: LicensePlate }): boolean {
  const markedUsStates = Object.keys(plates)
    .map(key => plates[key])
    .filter(plate => plate.country == 'US')
    .map(plate => plate.stateOrProvince);

  const markedWestCoastStates = markedUsStates
    .filter(state => westCoastUsStatesLkp.has(state));

  return isConnected(markedWestCoastStates,
    (state: string) => eastCoastStatesLkp.has(state),
    markedUsStates.map(state => state));
}

function isConnected(startingStates: string[],
  isMatchingBorderState: (state: string) => boolean,
  markedStates: string[]): boolean {

  const toVisit = startingStates
    .reduce((lkp, state) => lkp.add(state), new Set<string>());

  const markedStatesLookup = markedStates
    .reduce((lkp, state) => lkp.add(state), new Set<string>())

  const visitedStates = new Set<string>();

  while (toVisit.size > 0) {
    // dequeue state from the toVisit set
    const [first] = toVisit;
    toVisit.delete(first);

    // skip to next iteration if we have visited this state before
    if (visitedStates.has(first)) {
      continue;
    }

    // this is first time checking this state, mark it as visited
    visitedStates.add(first);

    // visit state borders.
    const currentStateBorders = UsStateBorders[first] ?? <StateBorder>{};
    const currentMarkedBorders = Object.keys(currentStateBorders)
      // ensure border state has been marked, and hasn't been marked for visiting
      .filter((borderState: string) => markedStatesLookup.has(borderState) &&
        !toVisit.has(borderState));

    for (const borderingState of currentMarkedBorders) {
      if (isMatchingBorderState(borderingState)) {
        // borders are connected
        return true;
      } else {
        // borders are not connected. enqueue state for a visit.
        toVisit.add(borderingState);
      }
    }
  }

  // borders are not connected
  return false;
}


export default function CalculateScore(currentGame: Game): void {
  // recalculate all scores
  currentGame.score = <ScoreData>{
    totalScore: 0,
    milestones: []
  };

  const spottedPlates = Object.keys(currentGame.licensePlates);

  let score = spottedPlates
    .reduce((currentScore, key) => {
      const multiplier = scopeMultiplierByPlateLkp.get(key) ?? 1;

      return currentScore + multiplier;
    }, 0);

  if (hasWestCoast(currentGame.licensePlates)) {
    score += 10;
    currentGame.score.milestones.push('West Coast');
  }

  if (hasEastCoast(currentGame.licensePlates)) {
    score += 20;
    currentGame.score.milestones.push('East Coast');
  }

  if (hasTransAtlantic(currentGame.licensePlates)) {
    score += 100;
    currentGame.score.milestones.push('Coast-to-Coast');
  }

  currentGame.score.totalScore = score;
}