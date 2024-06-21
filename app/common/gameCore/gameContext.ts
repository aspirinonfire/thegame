import { createContext } from "react";
import UserAccount from "../accounts";
import { Game } from "./gameModels";

export const CurrentUserAccountContext = createContext<UserAccount | null>(null);

export const CurrentGameContext = createContext({
  currentGame: <Game | null>null,
  setNewCurrentGame: (_newCurrentGame: Game | null): void  => {}
});