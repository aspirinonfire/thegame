import * as ort from 'onnxruntime-node';

const model_file = 'skl_plates_model.onnx';

const sess = await ort.InferenceSession.create(`public/${model_file}`);

console.log('inputs', sess.inputNames);
console.log('outputs', sess.outputNames);
console.log('meta', sess.inputMetadata);

const query = 'red top white middle blue bottom';

const feeds = {
  Text: new ort.Tensor('string', [ query ], [1, 1]),
  Label: new ort.Tensor('string', [ '' ], [1, 1]),
};

const results = await sess.run(feeds);
console.log(results);

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

  console.log(`\nTop ${k} for \"${query}\":`);
  for (const item of topK) {
    console.log(`${item.label}   ${item.probability.toString().substring(0, 5)}`);
  }
}