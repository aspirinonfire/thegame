import { OnnxPlateDescriptionClassifier, type ScoredLabel } from "~/common-components/plateDescriptionClassifier";
import type { StateCreator } from "zustand";
import type { AppStore } from "./AppStore";

export const onnxModel = "mlnet_plates_model.onnx";
export const onnxModelLabels = "mlnet_plates_model.labels.json"

export interface AppAiSearchSlice {
  plateClassifier: OnnxPlateDescriptionClassifier;
  getMatchingPlates: (searchQuery: string) => Promise<ScoredLabel[]>;
}

export const createAppAiSearchSlice: StateCreator<AppStore, [], [], AppAiSearchSlice> = (set, get) => ({
  plateClassifier: new OnnxPlateDescriptionClassifier(),

  getMatchingPlates: async (searchQuery: string) => {
    if (!searchQuery) {
      return [];
    }

    try {
      return await get().plateClassifier.predictAll(searchQuery);
    }
    catch (err) {
      console.error("Error during plate classification:", err)
    }
    return [];
  }
});