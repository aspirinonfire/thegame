import type { LicensePlateSpot } from "./LicensePlateSpot";
import type { ScoreData } from "./ScoreData";

export interface Game {
  gameId: number;
  gameName: string;
  createdByPlayerId: number;
  createdByPlayerName: string;
  dateCreated: Date;
  dateModified?: Date;
  endedOn?: Date;
  spottedPlates: LicensePlateSpot[];
  score: ScoreData;
}
