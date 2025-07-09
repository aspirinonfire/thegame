export type NextJsScoreMilestone = "Coast-to-Coast" | "West Coast" | "East Coast";

export interface NextJsScoreData {
  totalScore: number,
  milestones: NextJsScoreMilestone[]
}

export interface NextJsLicensePlateSpot {
  stateOrProvince: string,
  country: NextJsCountry,
  dateSpotted: Date | null,
  spottedBy: string | null
}

export interface NextJsGame {
  id: string,
  name: string,
  createdBy: string,
  dateCreated: Date,
  dateFinished?: Date,
  licensePlates: { [K: string]: NextJsLicensePlateSpot },
  score: NextJsScoreData
}

export type NextJsCountry = 'US' | 'CA' | 'MX'

export type NextJsTerritoryModifier = 'West Coast' | 'East Coast';

export interface Territory {
  shortName: string,
  longName: string,
  country: NextJsCountry,
  licensePlateImgs: string[],
  modifier?: NextJsTerritoryModifier[],
  scoreMultiplier?: number
}

export interface NextJsGameData {
  currentGame: NextJsGame | null,
  pastGames: NextJsGame[]
}