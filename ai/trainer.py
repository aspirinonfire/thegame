from pathlib import Path
from sklearn import clone
from sklearn.model_selection import train_test_split
import os

from data_loader import read_raw_data, transform_to_training_rows
from evaluator import compute_model_evaluations
from model_utils import export_to_onnx, print_top_k
from pipeline_factory import create_lr_pipeline;

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

  current_best_lr_params = {
    "random_state": random_state,
    "C": 8,
    "tol": 0.004,
    "max_iter": 50
  }

  lr_pipeline = create_lr_pipeline(**current_best_lr_params)
  print("Fitting LR...")
  lr_estimator = clone(lr_pipeline).fit(X_train, y_train)
  print("LR Fitted successfully.")
  lr_evals = compute_model_evaluations(lr_pipeline, lr_estimator, random_state, X_train, y_train, X_test, y_test)
  lr_evals.print()

  # current_best_svc_params = {
  #   "random_state": random_state,
  #   "C": 500,
  #   "cv": 5,
  #   "tol": 0.004,
  #   "max_iter": 100
  # }

  # svm_pipeline = create_svm_pipeline(**current_best_svc_params)
  # print("Fitting SVM...")
  # svm_estimator = clone(svm_pipeline).fit(X_train, y_train)
  # print("SVM Fitted successfully.")
  # svm_evals = compute_model_evaluations(svm_pipeline, svm_estimator, random_state, X_train, y_train, X_test, y_test)
  # svm_evals.print()

  print_top_k("red top white middle blue bottom", lr_estimator, 5)
  print_top_k("solid white plate", lr_estimator, 5)
  print_top_k("green top", lr_estimator, 5)
  print_top_k("green top white bottom", lr_estimator, 5)
  print_top_k("blue white plate", lr_estimator, 5)
  print_top_k("green background", lr_estimator, 3)
  print_top_k("green plate", lr_estimator, 3)
  print_top_k("solid green background", lr_estimator, 3)
  print_top_k("solid green plate", lr_estimator, 3)
  print_top_k("solid green", lr_estimator, 3)

  # get_feature_contribs(lr_estimator, ["us-al", "us-nh", "us-tn", "us-vt"], "green plate")
  # get_feature_contribs(lr_estimator, ["us-al", "us-nh", "us-tn", "us-vt"], "green background")

  # save to onnx
  export_to_onnx(lr_estimator, onnx_export_path)

if __name__ == "__main__":
  main()