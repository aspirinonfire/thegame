import * as ort from "onnxruntime-web";
ort.env.debug = true;
ort.env.logLevel = 'verbose';

export type ScoredLabel = { label: string; probability: number };

export class OnnxPlateDescriptionClassifier {
  private session!: ort.InferenceSession;
  private labels: string[] = [];

  public async init(modelUrl: string, labelsUrl: string): Promise<void> {
    this.session = await ort.InferenceSession.create(modelUrl, {
      executionProviders: ["wasm"]
    });
    
    // TODO optimize for offline + pwa caching (see version json in /about)
    // Load labels.
    const response = await fetch(labelsUrl, { cache: "force-cache" });
    if (!response.ok) {
      throw new Error(`Failed to load labels: ${response.status}`);
    }
    this.labels = await response.json();
  }

  debugDump(feeds: Record<string, ort.Tensor>) {
    console.group("ORT.run debug");

    const inputs = this.session.inputNames.map((n, i) => ({
      name: n,
      type: (this.session.inputMetadata as any[])[i]?.type,
      dims: (this.session.inputMetadata as any[])[i]?.dimensions ?? (this.session.inputMetadata as any[])[i]?.shape
    }));

    const outputs = this.session.outputNames.map((n, i) => ({
      name: n,
      type: (this.session.outputMetadata as any[])[i]?.type,
      dims: (this.session.outputMetadata as any[])[i]?.dimensions ?? (this.session.outputMetadata as any[])[i]?.shape
    }));

    const feedsJson = Object.entries(feeds).map(([k, t]) => ({
      name: k, type: t.type, dims: t.dims, size: t.data.length
    }));

    console.log(JSON.stringify({
      inputs,
      outputs,
      feeds: feedsJson
    },null, 2));

    console.groupEnd();
  }

  public async predictAll(query: string): Promise<ScoredLabel[]> {
    // const feeds: Record<string, ort.Tensor> = {
    //   Label: new ort.Tensor("string", [""], [1, 1]),
    //   Text: new ort.Tensor("string", [query], [1, 1]),
    //   Weight: new ort.Tensor("float32", new Float32Array([1]), [1, 1])
    // };

    const names = this.session.inputNames;                  // e.g., ["Label","Text","Weight"]
    const metas = this.session.inputMetadata as any[];      // array-like in your build
    const norm = (dims?: readonly number[]) => (dims?.length ? dims : [1])
      .map(d => (d && d > 0 && d < 4294967295 ? d : 1));


    const feeds: Record<string, ort.Tensor> = {};
    for (let i = 0; i < names.length; i++) {
      const name = names[i];
      const meta = metas[i];
      // const dims = norm(meta?.dimensions ?? meta?.shape);
      
      const dims = [1, 1];
      if (meta?.type === "string") {
        feeds[name] = new ort.Tensor("string", [name === "Text" ? query : ""], dims);
      } else {
        feeds[name] = new ort.Tensor("float32", new Float32Array([1]), dims);
      }
    }

    this.debugDump(feeds);

    const results = await this.session.run(feeds);
    const scoreTensor = results["Score.output"] as ort.Tensor;

    // Expect Float32Array of length == labels.length.
    const raw = Array.from(scoreTensor.data as Float32Array);

    // If not normalized, apply softmax defensively.
    const sum = raw.reduce((a, b) => a + b, 0);
    const probs = (sum > 1.0001 || sum < 0.9999)
      ? softmax(raw)
      : raw;

    const scored = this.labels.map((label, idx) => ({
      label,
      probability: probs[idx] ?? 0,
    }));

    // Sort descending for display.
    scored.sort((a, b) => b.probability - a.probability);
    return scored;
  }
}

function softmax(values: number[]): number[] {
  const maxVal = Math.max(...values);
  const exps = values.map(v => Math.exp(v - maxVal));
  const denom = exps.reduce((a, b) => a + b, 0);
  return exps.map(v => v / (denom === 0 ? 1 : denom));
}