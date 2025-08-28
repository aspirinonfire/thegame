from pathlib import Path
from typing import Any, Dict, List
from sklearn import clone
from sklearn.model_selection import train_test_split
import os

from data_loader import read_raw_data, transform_to_training_rows
from evaluator import compute_model_evaluations
from model_utils import export_to_onnx, print_top_k
from pipeline_factory import create_hyperparams_search, create_lr_pipeline;

def main():
  random_state = 500

  base_dir = Path(__file__).parent
  training_data_path = os.path.join(base_dir, "training_data", "plate_descriptions.json")
  onnx_export_path = os.path.join(base_dir, "..", "ui", "public", "skl_plates_model.onnx")

  json_data = read_raw_data(training_data_path)
  training_rows = transform_to_training_rows(json_data)
  X = training_rows["text"]
  y = training_rows["label"]
  X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.2, stratify=y, random_state=random_state
  )

  lr_pipeline = create_lr_pipeline()

  param_grid: Dict[str, List[Any]] = {
    # Vectorizer
    "tfidf__ngram_range": [(1, 1), (1, 2)],
    # Classifier
    "classifier__max_iter": [500],
    "classifier__tol": [0.001, 0.0001, 0.00001, 0.000001],
    "classifier__C": [10.0, 20.0, 50, 100, 200],
  }

  search = create_hyperparams_search(lr_pipeline, param_grid)

  search.fit(X_train, y_train)

  print(f"Best params (by log-loss):\n{search.best_params_}")
  print(f"Best CV log-loss: {search.best_score_:.4f} (negated; closer to 0 is better)")

  model_evals = compute_model_evaluations(lr_pipeline,
    search.best_estimator_,
    random_state,
    X_train,
    y_train,
    X_test,
    y_test)
  model_evals.print()

  print_top_k("red top white middle blue bottom", search.best_estimator_, 5)
  print_top_k("solid white plate", search.best_estimator_, 5)
  print_top_k("green top", search.best_estimator_, 5)
  print_top_k("green top white bottom", search.best_estimator_, 5)
  print_top_k("blue white plate", search.best_estimator_, 5)
  print_top_k("green background", search.best_estimator_, 3)
  print_top_k("green plate", search.best_estimator_, 3)
  print_top_k("solid green background", search.best_estimator_, 3)
  print_top_k("solid green plate", search.best_estimator_, 3)
  print_top_k("solid green", search.best_estimator_, 3)

  # get_feature_contribs(search.best_estimator_, ["us-al", "us-nh", "us-tn", "us-vt"], "green plate")
  # get_feature_contribs(search.best_estimator_, ["us-al", "us-nh", "us-tn", "us-vt"], "green background")

  # save to onnx
  export_to_onnx(search.best_estimator_, onnx_export_path)

if __name__ == "__main__":
  main()