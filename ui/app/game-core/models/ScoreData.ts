import type { TerritoryModifier } from "./Territory";

export type ScoreMilestone = TerritoryModifier | "Coast-to-Coast" | "Globetrotter";

export interface ScoreData {
  totalScore: number;
  achievements: ScoreMilestone[];
}