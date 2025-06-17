import { territories, territoryModifierScoreLookup } from "./gameConfiguration";
import type { LicensePlateSpot } from "./models/LicensePlateSpot";
import { type ScoreData } from "./models/ScoreData";
import type { Territory, TerritoryModifier } from "./models/Territory";

export const territoriesByKeyLkp = territories.reduce((map, ter) => {
  map.set(ter.key, ter);
  return map;
}, new Map<string, Territory>());

export function createTerritoriyModifierLookup(srcTerritories: Territory[]) {
  return srcTerritories
    .filter(ter => (ter.modifier ?? []).length > 0)
    .reduce((map, ter) => {
      for (const modifier of ter.modifier!) {
        const territoriesWithThisModifier = map.get(modifier) ?? new Set<TerritoryModifier>();
        territoriesWithThisModifier.add(ter.key);
        map.set(modifier, territoriesWithThisModifier);
      }

      return map;
    }, new Map<TerritoryModifier, Set<string>>());
}

export const territoriesByModifiers = createTerritoriyModifierLookup(territories);

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
    const currentMarkedBorders = currentStateBorders.values()
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

  const markedPlatesKeys = new Set<string>(allSpottedPlates.map(plate => plate.key));

  // apply score multiplier for all marked states
  const baseScore = allSpottedPlates
    .reduce((currentScore, plate) => {
      const multiplier = territoriesByKeyLkp.get(plate.key)?.scoreMultiplier ?? 1;
      return currentScore + multiplier;
    }, 0);
  scoreData.totalScore += baseScore;

  // apply milestone bonuses
  const markedPlatesByMilestones = createTerritoriyModifierLookup(allSpottedPlates
    .map(spot => territoriesByKeyLkp.get(spot.key))
    .filter(ter => !!ter)
  );

  for (const [modifier, territoriesInSet] of territoriesByModifiers) {
    const numOfSpottedPlatesWithCurrentModifier = markedPlatesByMilestones.get(modifier)?.size ?? 0;
    if (territoriesInSet.size === numOfSpottedPlatesWithCurrentModifier) {
      scoreData.milestones.push(modifier);
      scoreData.totalScore += territoryModifierScoreLookup.get(modifier) ?? 10;
    }
  }

  const markedWestCoastStates = [...markedPlatesByMilestones.get("West Coast") ?? []];
  const hasCoastToCoastConnection = AreBordersConnected(markedWestCoastStates,
    (state: string) => (territoriesByModifiers.get("East Coast") ?? new Set<string>()).has(state),
    markedPlatesKeys);

  if (hasCoastToCoastConnection) {
    scoreData.milestones.push("Coast-to-Coast");
    scoreData.totalScore += territoryModifierScoreLookup.get("Coast-to-Coast") ?? 10;
  }

  if (territories.length === allSpottedPlates.length) {
    scoreData.milestones.push("Globetrotter");
    scoreData.totalScore += territoryModifierScoreLookup.get("Globetrotter") ?? 1000;
  }

  return scoreData;
}