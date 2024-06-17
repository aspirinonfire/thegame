export type ScoreMilestone = "Coast-to-Coast" | "West Coast" | "East Coast";

export interface ScoreData {
  totalScore: number,
  milestones: ScoreMilestone[]
}

export interface LicensePlateSpot {
  stateOrProvince: string,
  country: Country,
  fullName: string,
  plateKey: string,
  plateImageUrl: string
  dateSpotted: Date | null,
  spottedBy: string | null
}

export interface Game {
  id: string,
  name: string,
  createdBy: string,
  dateCreated: Date,
  dateFinished?: Date,
  licensePlates: { [K: string]: LicensePlateSpot },
  score: ScoreData
}

export type Country = 'US' | 'CA' | 'MX'

export type TerritoryModifier = 'West Coast' | 'East Coast';

export interface Territory {
  shortName: string,
  longName: string,
  country: Country,
  licensePlateImgs: string[],
  modifier?: TerritoryModifier[],
  scoreMultiplier?: number
}

export interface StateBorder
{
  [key: string] : Border
}

export interface Border
{
  [Key: string]: boolean
}