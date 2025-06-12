import type { Country } from "./Country";

export interface LicensePlateSpot {
  stateOrProvince: string;
  country: Country;
  dateSpotted: Date | null;
  spottedBy: string | null;
}
