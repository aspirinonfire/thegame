import type { ApiSlice } from "./ApiSlice";
import type { AppAuthSlice } from "./AppAuthSlice";
import type { AppInitSlice } from "./AppInitSlice";
import type { GameSlice } from "./GameSlice";

export declare type AppStore = AppInitSlice & ApiSlice & AppAuthSlice & GameSlice;
