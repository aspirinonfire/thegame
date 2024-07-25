export interface PlayerInfo {
  playerName: string,
  playerId: number
}

export interface UserDetails {
  isAuthenticated: boolean
  Player: PlayerInfo
}