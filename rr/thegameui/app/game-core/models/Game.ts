import type { LicensePlateSpot } from "./LicensePlateSpot";
import type { ScoreData } from "./ScoreData";

export interface Game {
  id: string;
  name: string;
  createdBy: string;
  dateCreated: Date;
  dateFinished?: Date;
  licensePlates: { [K: string]: LicensePlateSpot; };
  score: ScoreData;
}
