from pathlib import Path
from typing import Any, Dict, List
from sklearn.model_selection import train_test_split
import os
import argparse

from sklearn.pipeline import Pipeline

from data_loader import read_raw_data, transform_to_training_rows
from evaluator import compute_model_evaluations
from hyperparam_manager import find_best_lr_params, load_hyperparams_from_file, save_hyperparams_to_file
from model_utils import export_to_onnx, print_top_k
from pipeline_factory import CLF_STEP, VEC_STEP, create_lr_pipeline;

def main(use_search: bool,
  training_data_path: str,
  lr_training_params_path: str,
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
      lr_training_params_path=lr_training_params_path)
  else:
    final_estimator = create_lr_estimator_from_precomputed_params(X_train=X_train,
      X_test=X_test,
      y_train=y_train,
      y_test=y_test,
      random_state=random_state,
      lr_training_params_path=lr_training_params_path)

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

  # save to onnx
  export_to_onnx(final_estimator, onnx_export_path)


def create_lr_estimator_from_precomputed_params(X_train: Any,
  X_test: Any,
  y_train: Any,
  y_test: Any,
  random_state: int,
  lr_training_params_path: str) -> Pipeline:
  
  lr_pipeline = create_lr_pipeline(random_state, max_iter=1000)

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
  lr_training_params_path: str) -> Pipeline:
  
  lr_pipeline = create_lr_pipeline(random_state, max_iter=1000)

  param_grid: Dict[str, List[Any]] = {
    # Vectorizer
    f"{VEC_STEP}__ngram_range": [(1, 1), (1, 2)],
    f"{VEC_STEP}__use_idf": [True, False],
    f"{VEC_STEP}__norm": [None],
    f"{VEC_STEP}__sublinear_tf": [False],
    # Classifier
    f"{CLF_STEP}__solver": ["lbfgs", "saga"],
    f"{CLF_STEP}__tol": [0.001, 0.0005, 0.0001, 0.00005],
    f"{CLF_STEP}__C": [5.0, 7.0, 8.0, 9.0, 10.0, 20.0, 50.0, 70.0],
    f"{CLF_STEP}__class_weight": [None]
  }

  search_results = find_best_lr_params(pipeline=lr_pipeline,
    X_train=X_train,
    X_test=X_test,
    y_train=y_train,
    y_test=y_test,
    random_state=random_state,
    param_grid=param_grid,
    refit="ndcg")
  
  search_results.print_results()
  save_hyperparams_to_file(lr_training_params_path, search_results.to_model_params())

  return search_results.best_estimator

def parse_args() -> argparse.Namespace:
  argument_parser = argparse.ArgumentParser()
  argument_parser.add_argument(
      "--use-search",
      action="store_true",
      help="Enable grid search to find best hyperparams (default: False)."
  )
  return argument_parser.parse_args()


if __name__ == "__main__":
  parsed_args = parse_args()

  base_dir = Path(__file__).parent
  training_data_path = os.path.join(base_dir, "training_data", "plate_descriptions.json")
  lr_training_params_path = os.path.join(base_dir, "lr_training_params.json")
  onnx_export_path = os.path.join(base_dir, "..", "ui", "public", "skl_plates_model.onnx")

  main(use_search=parsed_args.use_search,
    training_data_path=training_data_path,
    lr_training_params_path=lr_training_params_path,
    onnx_export_path=onnx_export_path)