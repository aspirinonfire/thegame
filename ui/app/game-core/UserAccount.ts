export interface PlayerInfo {
  playerName: string,
  playerId: number
}

export default interface UserAccount {
  isAuthenticated: boolean;
  player: PlayerInfo
}
