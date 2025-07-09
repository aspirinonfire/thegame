export interface ScoreData {
  totalScore: number,
  achievements: string[]
}

export interface LicensePlateSpot {
  stateOrProvince: string,
  country: Country,
  spottedOn: Date | null,
  spottedByPlayerName: string | null
  spottedByPlayerId: number | null
}

export interface NewSpottedPlate {
  country: Country,
  // TODO enum
  stateOrProvince: string
}

export interface Game {
  gameId: string,
  gameName: string,
  isOwner: boolean,
  createdByPlayerName: string,
  dateCreated: Date,
  endedOn?: Date,
  spottedPlates: LicensePlateSpot[]
  gameScore: ScoreData
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