import pandas as pd
from skl2onnx import to_onnx
from skl2onnx.common.data_types import StringTensorType
from sklearn.calibration import label_binarize
from sklearn.metrics import ndcg_score
from sklearn.model_selection import GridSearchCV
from sklearn.pipeline import Pipeline

from pipeline_factory import CLF_STEP

# Print query predictions against trained model
def print_top_k(query_text: str, estimator: Pipeline, top_k: int = 5):
  labels = estimator.named_steps[CLF_STEP].classes_

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
      id(estimator.named_steps[CLF_STEP]): {"zipmap": False, "output_class_labels": True}
    }
  )

  with open(export_path, "wb") as f:
    f.write(onnx.SerializeToString())

  print("Exported to ONNX successfully")

def make_ndcg_scorer(k=10):
  def _scorer(estimator, X, y):
    proba = estimator.predict_proba(X)
    y_true_bin = label_binarize(y, classes=estimator.classes_)
    k_eff = min(k, proba.shape[1])
    return float(ndcg_score(y_true_bin, proba, k=k_eff))
  return _scorer