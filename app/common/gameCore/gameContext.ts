import { createContext } from "react";
import UserAccount from "../accounts";
import { Game } from "./gameModels";

export const CurrentUserAccountContext = createContext<UserAccount | null>(null);

export interface GameContext {
  activeGame : Game | null,
  setActiveGame: (newActiveGame: Game | null) => void
}

export const CurrentGameContext = createContext<GameContext>({ activeGame: null, setActiveGame: (game) => {} } as GameContext);