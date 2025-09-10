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
  private labels!: string[];

  public async init(modelUrl: string, labelUrl: string): Promise<void> {
    this.session = await ort.InferenceSession.create(modelUrl, {
      executionProviders: ["wasm"],
      logSeverityLevel: 3, // error
      logVerbosityLevel: 0
    });
    this.labels = await (await fetch(labelUrl)).json();
  }

  public async predictAll(query: string): Promise<ScoredLabel[]> {
    const feeds = {
      Text: new ort.Tensor('string', [ query ], [1, 1]),
      Label: new ort.Tensor('string', [''], [1, 1])
    };

    const results = await this.session.run(feeds);

    const probabilities = Array.from(await results['Score.output'].getData() as Float64Array);

    return probabilities
      .map((prob, idx) => ({
        label: this.labels[idx],
        probability: prob
      }))
      .sort((a, b) => b.probability - a.probability);
  }
}