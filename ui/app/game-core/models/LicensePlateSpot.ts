export interface LicensePlateSpot {
  key: string;
  country: string;
  stateOrProvince: string;
  spottedOn: Date | null;
  spottedByPlayerName: string | null;
  spottedByPlayerId: number | null;
  // Optional: when selected via AI search, attach its prompt
  mlPrompt?: string;
}
