import { UsStateBorders, mockGameData } from "../data";
import { LicensePlate, ScoreData, StateBorder } from "./gameModels";

// west-coast states lookup
const westCoastUsStatesLkp: Set<string> = mockGameData
  .filter(state => !!state.modifier && state.modifier.indexOf('West Coast') >= 0)
  .map(state => state.shortName)
  .reduce((lkp, state) => lkp.add(state), new Set<string>());

// east-coast states lookup
const eastCoastStatesLkp: Set<string> = mockGameData
  .filter(state => !!state.modifier && state.modifier?.indexOf('East Coast') >= 0)
  .map(state => state.shortName)
  .reduce((lkp, state) => lkp.add(state), new Set<string>());

// score multiplier by state lookup
const scopeMultiplierByPlateLkp: Map<string, number> = mockGameData
  .reduce((lkp, territory) => {
    lkp.set(`${territory.country}-${territory.shortName}`, territory.scoreMultiplier ?? 1);
    return lkp;
  }, new Map<string, number>());

/**
 * Check if borders are connected using Breadth-First Search Graph algorithm
 * @param startingFromBorder graph roots (eg., West Coast States)
 * @param isMatchingOtherBorder expected leaf nodes to have a connection (eg., East Coast States)
 * @param markedStates "graph"
 * @returns 
 */
function AreBordersConnected(startingFromBorder: string[],
  isMatchingOtherBorder: (state: string) => boolean,
  markedStates: string[]): boolean {

  const toVisit = startingFromBorder
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
      if (isMatchingOtherBorder(borderingState)) {
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

// Calculate score based on the spotted plates
export default function CalculateScore(plateData: LicensePlate[]): ScoreData {
  const scoreData: ScoreData = {
    totalScore: 0,
    milestones: []
  };

  if (plateData === null) {
    return scoreData;
  }

  const allSpottedPlates = plateData
    .filter(plate => !!plate.dateSpotted);

  if (allSpottedPlates.length < 1) {
    return scoreData;
  }

  const markedUsStates = allSpottedPlates
    .filter(plate => plate.country == 'US')
    .map(plate => plate.stateOrProvince);

  const markedStatesSet = new Set<string>(markedUsStates);

  // apply score multiplier for all marked states
  const baseScore = allSpottedPlates
    .reduce((currentScore, plate) => {
      const key = `${plate.country}-${plate.stateOrProvince}`;
      const multiplier = scopeMultiplierByPlateLkp.get(key) ?? 1;
      return currentScore + multiplier;
    }, 0);
  scoreData.totalScore += baseScore;

  // apply milestone bonuses
  const hasAllWestCoastMarked = [...westCoastUsStatesLkp]
    .every(westCoastState => markedStatesSet.has(westCoastState));

  if (hasAllWestCoastMarked) {
    scoreData.totalScore += 10;
    scoreData.milestones.push('West Coast');
  }

  const hasAllEastCoastMarked = [...eastCoastStatesLkp]
    .every(eastCoastState => markedStatesSet.has(eastCoastState));

  if (hasAllEastCoastMarked) {
    scoreData.totalScore += 50;
    scoreData.milestones.push('East Coast');
  }

  const markedWestCoastStates = markedUsStates
    .filter(state => westCoastUsStatesLkp.has(state));

  const hasCoastToCoastConnection = AreBordersConnected(markedWestCoastStates,
    (state: string) => eastCoastStatesLkp.has(state),
    markedUsStates);

  if (hasCoastToCoastConnection) {
    scoreData.totalScore += 100;
    scoreData.milestones.push('Coast-to-Coast');
  }

  return scoreData;
}