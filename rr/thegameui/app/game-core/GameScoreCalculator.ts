import { eastCoastStates, territoriesByKeyLkp, westCoastStates } from "./gameConfiguration";
import type { LicensePlateSpot } from "./models/LicensePlateSpot";
import type { ScoreData } from "./models/ScoreData";

/**
 * Check if borders are connected using Breadth-First Search Graph algorithm
 * @param startingFromBorder graph roots (eg., West Coast States)
 * @param isMatchingOtherBorder expected leaf nodes to have a connection (eg., East Coast States)
 * @param markedStates "graph"
 * @returns 
 */
function AreBordersConnected(startingFromBorder: string[],
  isMatchingOtherBorder: (state: string) => boolean,
  markedStatesLookup: Set<string>): boolean {

  const toVisit = startingFromBorder
    .reduce((lkp, state) => lkp.add(state), new Set<string>());

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
    const currentStateBorders = territoriesByKeyLkp.get(first)?.borders ?? new Set<string>();
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

/**
 * Calculate current game score
 * @param plateData 
 * @returns 
 */
export default function CalculateScore(plateData: LicensePlateSpot[]): ScoreData {
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

  const markedPlates = new Set<string>(allSpottedPlates.map(plate => plate.key));

  // apply score multiplier for all marked states
  const baseScore = allSpottedPlates
    .reduce((currentScore, plate) => {
      const multiplier = territoriesByKeyLkp.get(plate.key)?.scoreMultiplier ?? 1;
      return currentScore + multiplier;
    }, 0);
  scoreData.totalScore += baseScore;

  // apply milestone bonuses
  const hasAllWestCoastMarked = [...westCoastStates]
    .every(key => markedPlates.has(key));

  if (hasAllWestCoastMarked) {
    scoreData.totalScore += 10;
    scoreData.milestones.push('West Coast');
  }

  const hasAllEastCoastMarked = [...eastCoastStates]
    .every(key => markedPlates.has(key));

  if (hasAllEastCoastMarked) {
    scoreData.totalScore += 50;
    scoreData.milestones.push('East Coast');
  }

  const markedWestCoastStates = [...markedPlates]
    .filter(key => westCoastStates.has(key));

  const hasCoastToCoastConnection = AreBordersConnected(markedWestCoastStates,
    (state: string) => eastCoastStates.has(state),
    markedPlates);

  if (hasCoastToCoastConnection) {
    scoreData.totalScore += 100;
    scoreData.milestones.push('Coast-to-Coast');
  }

  return scoreData;
}