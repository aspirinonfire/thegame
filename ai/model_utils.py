import pandas as pd
from skl2onnx import to_onnx
from skl2onnx.common.data_types import StringTensorType
from sklearn.pipeline import Pipeline

# Print query predictions against trained model
def print_top_k(query_text: str, estimator: Pipeline, top_k: int = 5):
  labels = estimator.named_steps["classifier"].classes_

  raw_probs = estimator.predict_proba([query_text])[0]
  
  probabilities = (
    pd.DataFrame({"label": labels, "probability": raw_probs})
      .sort_values("probability", ascending=False)
      .reset_index(drop=True)
  )

  print(f"== \"{query_text}\" ==")
  print(probabilities.head(top_k))

def export_to_onnx(estimator: Pipeline, export_path: str):
  print(f"Exporting estimator to {export_path}...")

  onnx = to_onnx(
    estimator,
    # define inference input for onnxruntime
    initial_types=[("text", StringTensorType([None, 1]))],
    options={
      # zipmap is not well supported in onnxruntime-web
      id(estimator.named_steps["classifier"]): {"zipmap": False, "output_class_labels": True}
    }
  )

  with open(export_path, "wb") as f:
    f.write(onnx.SerializeToString())

  print("Exported to ONNX successfully")