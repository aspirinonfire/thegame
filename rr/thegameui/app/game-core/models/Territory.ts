import type { Country } from "./Country";
import type { TerritoryModifier } from "./TerritoryModifier";

export interface Territory {
  shortName: string;
  longName: string;
  country: Country;
  licensePlateImgs: string[];
  modifier?: TerritoryModifier[];
  scoreMultiplier?: number;
}
