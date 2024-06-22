import { createContext } from "react";
import UserAccount from "../accounts";
import { Game } from "./gameModels";

export const CurrentUserAccountContext = createContext<UserAccount | null>(null);