export type ScoreMilestone = "Coast-to-Coast" | "West Coast" | "East Coast";

export interface ScoreData {
  totalScore: number;
  milestones: ScoreMilestone[];
}
