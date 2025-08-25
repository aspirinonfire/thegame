import * as ort from 'onnxruntime-node';

const model_file = "skl_plates_model.onnx";

const sess = await ort.InferenceSession.create(`public/${model_file}`);

console.log('inputs', sess.inputNames);
console.log('outputs', sess.outputNames);
console.log('meta', sess.inputMetadata);

const feeds = {
  text: new ort.Tensor('string', [ 'top red white middle blue bottom' ], [1, 1]),
};

const results = await sess.run(feeds);

const probabilitiesTensor = results.probabilities; // ort.Tensor
const classLabels = Array.from(results.class_labels.data);
const probabilitiesData = probabilitiesTensor.data;
// note batchSize depends on input feed. It is possible to query multiple strings at one time
const [batchSize, classCount] = probabilitiesTensor.dims;
const k = 5;

for (let batchIndex = 0; batchIndex < batchSize; batchIndex += 1) {
  const start = batchIndex * classCount;
  const row = Array.from(probabilitiesData.slice(start, start + classCount));

  const pairs = classLabels.map((label, index) => ({
    label,
    probability: row[index],
  }));
  pairs.sort((a, b) => b.probability - a.probability);

  const topK = pairs.slice(0, k);

  console.log(`\nTop ${k} for batch ${batchIndex}:`);
  for (const item of topK) {
    console.log(`${item.label.padEnd(4)}  ${item.probability}`);
  }
}