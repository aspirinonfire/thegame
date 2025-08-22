import type { AppApiSlice } from "./AppApiSlice";
import type { AppAuthSlice } from "./AppAuthSlice";
import type { AppInitSlice } from "./AppInitSlice";
import type { AppGameSlice } from "./AppGameSlice";
import type { AppAiSearchSlice } from "./AppAiSearchSlice";

export declare type AppStore = AppInitSlice & AppApiSlice & AppAuthSlice & AppGameSlice & AppAiSearchSlice;
