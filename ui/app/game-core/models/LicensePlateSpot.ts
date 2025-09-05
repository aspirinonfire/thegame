export interface LicensePlateSpot {
  key: string;
  country: string;
  stateOrProvince: string;
  spottedOn: Date | null;
  spottedByPlayerName: string | null;
  spottedByPlayerId: number | null;
  // When selected via AI search, attach its prompt
  mlPrompt?: string | null;
}
