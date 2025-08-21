import * as ort from "onnxruntime-web/wasm";

// ort.env.debug = true;
// ort.env.logLevel = 'verbose';
// ort.env.wasm.wasmPaths = {
//   wasm: "ort-wasm-simd-threaded.wasm",
//   mjs: "ort-wasm-simd-threaded.mjs",
// }


export type ScoredLabel = { label: string; probability: number };

export class OnnxPlateDescriptionClassifier {
  private session!: ort.InferenceSession;

  public async init(modelUrl: string): Promise<void> {
    this.session = await ort.InferenceSession.create(modelUrl, {
      executionProviders: ["wasm"]
    });
  }

  public async predictAll(query: string): Promise<ScoredLabel[]> {
    const feeds = {
      text: new ort.Tensor('string', [ query ], [1, 1]),
    };

    const results = await this.session.run(feeds);

    const probabilities = Array.from(await results.probabilities.getData() as Float64Array);
    const labels = Array.from(await results.class_labels.getData() as string[]);

    return probabilities
      .map((prob, idx) => ({
        label: labels[idx],
        probability: prob
      }))
      .sort((a, b) => b.probability - a.probability);
  }
}