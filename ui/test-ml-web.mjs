import * as ort from 'onnxruntime-node';

const model_file = "sdca_plates_model.onnx";
const label_json = "sdca_plates_labels.json";

const sess = await ort.InferenceSession.create(`public/${model_file}`);

console.log('inputs', sess.inputNames);
console.log('outputs', sess.outputNames);

const feeds = {
  Text: new ort.Tensor('string', [ 'top red white middle blue bottom' ], [1, 1]),
  Label: new ort.Tensor('string', [''], [1,1]),
  Weight: new ort.Tensor('float32', new Float32Array([1]), [1,1]),
};

const results = await sess.run(feeds, ['Score.output']);
console.log('ok:', results['Score.output'].dims);