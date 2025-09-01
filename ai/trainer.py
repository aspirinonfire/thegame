from pathlib import Path
from random import uniform
from typing import Any, Dict, List
import numpy as np
from sklearn.model_selection import train_test_split
import os
import argparse
from scipy.stats import loguniform

from sklearn.pipeline import Pipeline

from data_loader import read_raw_data, transform_to_training_rows
from evaluator import compute_model_evaluations
from hyperparam_manager import find_best_params_random_halving_search, load_hyperparams_from_file, save_hyperparams_to_file
from model_utils import export_to_onnx, print_top_k
from pipeline_factory import CLF_STEP, VEC_STEP, create_lr_pipeline, create_svm_pipeline;

def main(use_search: bool,
  training_data_path: str,
  training_params_path: str,
  onnx_export_path: str):
  
  random_state = 500

  json_data = read_raw_data(training_data_path)
  training_rows = transform_to_training_rows(json_data)
  X = training_rows["text"]
  y = training_rows["label"]
  X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.2, stratify=y, random_state=random_state
  )

  if (use_search):
    final_estimator = create_lr_estimator_from_search(X_train=X_train,
      X_test=X_test,
      y_train=y_train,
      y_test=y_test,
      random_state=random_state,
      training_params_path=training_params_path)
  else:
    final_estimator = create_lr_estimator_from_precomputed_params(X_train=X_train,
      X_test=X_test,
      y_train=y_train,
      y_test=y_test,
      random_state=random_state,
      lr_training_params_path=training_params_path)
    
    # save to onnx
    export_to_onnx(final_estimator, onnx_export_path)

  # Sanity check queries
  print_top_k("red top white middle blue bottom", final_estimator, 5)
  print_top_k("solid white plate", final_estimator, 5)
  print_top_k("green top", final_estimator, 5)
  print_top_k("green top white bottom", final_estimator, 5)
  print_top_k("blue white plate", final_estimator, 5)
  print_top_k("green background", final_estimator, 3)
  print_top_k("green plate", final_estimator, 3)
  print_top_k("solid green background", final_estimator, 3)
  print_top_k("solid green plate", final_estimator, 3)
  print_top_k("solid green", final_estimator, 3)

  # get_feature_contribs(estimator, ["us-al", "us-nh", "us-tn", "us-vt"], "green plate")
  # get_feature_contribs(estimator, ["us-al", "us-nh", "us-tn", "us-vt"], "green background")


def create_lr_estimator_from_precomputed_params(X_train: Any,
  X_test: Any,
  y_train: Any,
  y_test: Any,
  random_state: int,
  lr_training_params_path: str) -> Pipeline:
  
  lr_pipeline = create_lr_pipeline()

  print(f"Reading precomputed params from {lr_training_params_path}...")
  precomp_params = load_hyperparams_from_file(lr_training_params_path)
  print(f"Found: {precomp_params}")

  lr_pipeline.set_params(**precomp_params)

  print("Fitting...")
  estimator = lr_pipeline.fit(X=X_train, y=y_train)
  print("Fitted!")

  model_evals = compute_model_evaluations(lr_pipeline,
    estimator,
    random_state,
    X_train,
    y_train,
    X_test,
    y_test)
  
  model_evals.print()

  return estimator


def create_lr_estimator_from_search(X_train: Any,
  X_test: Any,
  y_train: Any,
  y_test: Any,
  random_state: int,
  training_params_path: str) -> Pipeline:
  
  lr_pipeline = create_lr_pipeline()

  lr_param_distr: Dict[str, List[Any]] = {
    # Vectorizer
    f"{VEC_STEP}__ngram_range": [(1, 1)],
    f"{VEC_STEP}__use_idf": [True, False],
    f"{VEC_STEP}__norm": [None],
    f"{VEC_STEP}__sublinear_tf": [True, False],
    # Classifier
    f"{CLF_STEP}__solver": ["lbfgs", "saga"],
    f"{CLF_STEP}__random_state": [random_state],
    f"{CLF_STEP}__tol": loguniform(0.00001, 0.01),
    f"{CLF_STEP}__C": loguniform(0.00001, 1000),
    f"{CLF_STEP}__class_weight": [None, "balanced"]
  }

  search_results = find_best_params_random_halving_search(pipeline=lr_pipeline,
    X_train=X_train,
    X_test=X_test,
    y_train=y_train,
    y_test=y_test,
    random_state=random_state,
    param_distributions=lr_param_distr,
    resource=f"{CLF_STEP}__max_iter",
    min_resources=100,
    max_resources=10000,
    refit="ndcg")
  
  search_results.print_results()
  save_hyperparams_to_file(training_params_path, search_results.to_model_params())

  return search_results.best_estimator

def create_svm_estimator_from_search(X_train: Any,
  X_test: Any,
  y_train: Any,
  y_test: Any,
  random_state: int,
  training_params_path: str) -> Pipeline:

  svc_pipeline = create_svm_pipeline()
  svc_param_distr: Dict[str, List[Any]] = {
    # Vectorizer
    f"{VEC_STEP}__ngram_range": [(1, 1), (1, 2)],
    f"{VEC_STEP}__use_idf": [True, False],
    f"{VEC_STEP}__norm": [None],
    f"{VEC_STEP}__sublinear_tf": [True, False],
    # Classifier
    f"{CLF_STEP}__estimator__kernel": ["poly", "rbf", "sigmoid"],
    f"{CLF_STEP}__estimator__gamma": list(np.logspace(-5, 0, 100)) + ["scale", "auto"],
    f"{CLF_STEP}__estimator__C": loguniform(0.001, 1000),
    f"{CLF_STEP}__estimator__tol": loguniform(0.00001, 0.001),
    f"{CLF_STEP}__estimator__degree": [2, 3, 4],
    f"{CLF_STEP}__estimator__class_weight": [None, "balanced"],
    f"{CLF_STEP}__estimator__coef0": [0, 1, 10],
    f"{CLF_STEP}__estimator__random_state": [random_state],
    # Calibration method
    f"{CLF_STEP}__method": ["sigmoid", "isotonic"],
    f"{CLF_STEP}__cv": [5],
  }

  search_results = find_best_params_random_halving_search(pipeline=svc_pipeline,
    X_train=X_train,
    X_test=X_test,
    y_train=y_train,
    y_test=y_test,
    random_state=random_state,
    param_distributions=svc_param_distr,
    resource=f"{CLF_STEP}__estimator__max_iter",
    min_resources=1000,
    max_resources=30000,
    refit="ndcg")
  
  search_results.print_results()
  save_hyperparams_to_file(training_params_path, search_results.to_model_params())

  return search_results.best_estimator

def parse_args() -> argparse.Namespace:
  base_dir = Path(__file__).parent
  training_data_path = os.path.join(base_dir, "training_data", "plate_descriptions.json")
  training_params_path = os.path.join(base_dir, "training_params.json")
  onnx_export_path = os.path.join(base_dir, "..", "ui", "public", "skl_plates_model.onnx")

  argument_parser = argparse.ArgumentParser(
    prog="License Plate AI search trainer",
    description="This program designed to train a License Plate search predictor/ranking model"
  )
  
  argument_parser.add_argument(
    "--use-search",
    action="store_true",
    help="Enable grid search to find best hyperparams (default: False)."
  )

  argument_parser.add_argument(
    "--data-path",
    default=training_data_path,
    help="path to training data json file"
  )

  argument_parser.add_argument(
    "--params-path",
    default=training_params_path,
    help="path to parameters json file"
  )

  argument_parser.add_argument(
    "--onnx-path",
    default=onnx_export_path,
    help="trained model output path in ONNX format"
  )

  return argument_parser.parse_args()

if __name__ == "__main__":
  parsed_args = parse_args()

  main(use_search=parsed_args.use_search,
    training_data_path=parsed_args.data_path,
    training_params_path=parsed_args.params_path,
    onnx_export_path=parsed_args.onnx_path)