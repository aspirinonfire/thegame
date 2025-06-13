import type { Country } from "./Country";

export type TerritoryModifier = 'West Coast' | 'East Coast';

export interface Territory {
  key: string;
  shortName: string;
  longName: string;
  country: Country;
  modifier?: TerritoryModifier[];
  scoreMultiplier?: number;
  borders: Set<string>
}
