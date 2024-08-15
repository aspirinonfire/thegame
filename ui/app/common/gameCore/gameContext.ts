import { createContext } from "react";
import { UserDetails } from "../accounts";
import { Game } from "./gameModels";

export interface UserDetailsContext {
  userDetails: UserDetails,
  setUserDetails: (details: UserDetails) => void
}

export interface GameContext {
  activeGame : Game | null,
  setActiveGame: (newActiveGame: Game | null) => void
}

export const CurrentUserAccountContext = createContext<UserDetailsContext>({
  userDetails: { 
    isAuthenticated: false,
    Player: {
      playerName: "Guest",
      playerId: -1
    }
  },
  setUserDetails: (details) => {}});

export const CurrentGameContext = createContext<GameContext>({
  activeGame: null,
  setActiveGame: (game) => {}});