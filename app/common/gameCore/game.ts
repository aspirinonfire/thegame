import { Country } from './gameDataTerritory';

export type ScoreMilestone = "Coast-to-Coast" | "West Coast" | "East Coast";

export interface ScoreData {
  totalScore: number,
  milestones: ScoreMilestone[]
}

export interface LicensePlate {
  stateOrProvince: string,
  country: Country,
  dateSpotted: Date | null,
  spottedBy: string | null
}

export interface Game {
  id: string,
  name: string,
  createdBy: string,
  dateCreated: Date,
  dateFinished?: Date,
  licensePlates: { [K: string]: LicensePlate },
  score: ScoreData
}