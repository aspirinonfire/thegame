from dataclasses import dataclass
import pandas as pd
from sklearn import clone
from sklearn.pipeline import Pipeline
from sklearn.model_selection import train_test_split
import os;
from pathlib import Path

@dataclass
class RawTrainingDataRow:
  key: str
  version: str
  weight: float
  description: dict[str, str]

# Read training data json and transform it into de-normalized training rows.
# Note: we are loading entire data set into memory because it is small enough and makes rest of the code simpler.
# For larger data sets, we'll need to refactor solution to work with streams, and de-normalize once into a temp file.
def read_raw_data(training_data_path: str) -> list[RawTrainingDataRow]:
  import json;
  
  print("Reading raw json data...")

  training_data_json = Path(training_data_path)

  with training_data_json.open("r", encoding="utf-8") as file:
    raw_json_data = json.load(file)

  return [RawTrainingDataRow(**item) for item in raw_json_data]

# Generate training rows by de-normalizing source json data
# { "key": "us-ca", "description": { "plate" : "white, solid white" }} =>
# { "label": "us-ca", "text": "white plate" }
# { "label": "us-ca", "text": "solid white plate" }
def transform_to_training_rows(training_data: list[RawTrainingDataRow]) -> pd.DataFrame:
  from itertools import product

  synonyms_lkp = {
    "top": ["upper"],
    "middle": ["center"],
    "bottom": ["lower"],
    "line": ["strip", "banner"],
    "lines": ["strips", "banners"],
    "solid": ["all"]
  }

  print("Transforming to training rows...")
  
  training_row_df = pd.DataFrame(training_data)
  training_row_df = training_row_df[training_row_df["key"] != "sample"]

  def create_training_text(plate_descriptions: dict[str, str]) -> list[str]:
    rows: list[str] = []

    for desc_key, desc_val in plate_descriptions.items():
      if not isinstance(desc_val, str):
        continue

      for raw_phrase in (p.strip() for p in desc_val.split(",") if p.strip()):
        phrase_variants_parts: list[list[str]] = []
        
        for word in raw_phrase.split(" "):
          word_synonyms = synonyms_lkp.get(word, [word])
          phrase_variants_parts.append(word_synonyms)
        
        for parts in product(*phrase_variants_parts):
          phrase = " ".join(parts)
          rows.append(phrase)
          rows.append(f"{desc_key} {phrase}")
          rows.append(f"{phrase} {desc_key}")

    return rows

  training_rows = (
    training_row_df
      .rename(columns={"key": "label"})
      .assign(
        text = lambda frame: frame["description"].apply(create_training_text)
      )
      .explode("text", ignore_index=True)[["label", "text"]]
  )

  print(f"Created {len(training_rows)} training rows.")

  return training_rows

# Train data set
# Tuned values as of 2025-08-26
# Logistic Regression + LBFGS:
# C=8, tol=0.004, max_iter=30
# ll-red: 0.4559, ROC AUC: .991 train/0.935 test
#
# Logistic Regression + SAGA:
# C=8, tol=0.01, max_iter=30
# ll-red: 0.4421, ROC AUC: .992 train/.934 test
#
# Logistic Regression + newton-cg:
# C=8, tol=0.001, max_iter=30
# ll-red: 0.4397, ROC AUC: .992 train/.934 test
#
# SGDClassifier and CalibratedClassifierCV could not get ll-reduction above 0.40
#
# Dataset is too small to benefit from approximation/online methods and there's
# enough class imbalance that causes probability calibration to become fragile.
def create_pipeline(random_state: int = 42,
    C: int = 8,
    tol: float = 0.004,
    max_iter: int = 30) -> Pipeline:
  from skl2onnx.sklapi import TraceableTfidfVectorizer # required for ONNX export!
  from skl2onnx import update_registered_converter
  from skl2onnx.shape_calculators.text_vectorizer import calculate_sklearn_text_vectorizer_output_shapes
  from skl2onnx.operator_converters.tfidf_vectoriser import convert_sklearn_tfidf_vectoriser
  from sklearn.linear_model import LogisticRegression

  print("Building training pipeline...")
  print(f"C={C}, tol={tol}, max_iter={max_iter}")

  # We need to use TraceableTfidfVectorizer from skl2onnx so we can convert
  # vectorizer to ONNX later. See https://github.com/scikit-learn/scikit-learn/issues/13733
  update_registered_converter(
    TraceableTfidfVectorizer,
    "Skl2onnxTraceableTfidfVectorizer",
    calculate_sklearn_text_vectorizer_output_shapes,
    convert_sklearn_tfidf_vectoriser,
    options={
      "tokenexp": None,
      "separators": None,
      "nan": [True, False],
      "keep_empty_string": [True, False],
      "locale": None,
    },
  )

  tfidf = TraceableTfidfVectorizer(
    ngram_range=(1, 4),          # 1–4 n-grams
    lowercase=True,
    token_pattern=r"[a-z0-9-]+", # word tokenization (words only no spaces)
    min_df=1,
    stop_words=None)

  # see https://scikit-learn.org/stable/modules/generated/sklearn.linear_model.LogisticRegression.html
  clf = LogisticRegression(
    solver="lbfgs",
    max_iter=max_iter,
    # ignored for non-stochastic
    random_state=random_state,
    penalty="l2",
    C=C,
    # convergence tolerance
    tol=tol,
    verbose=0,
    # use all cores
    n_jobs=1)

  pipeline = Pipeline(steps=[
    ("tfidf", tfidf),
    ("classifier", clf)
  ])

  return pipeline

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
  from skl2onnx import to_onnx
  from skl2onnx.common.data_types import StringTensorType

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

def evaluate_model(pipeline: Pipeline,
  fitted_estimator: Pipeline,
  random_state: int,
  X_train: list[str],
  y_train: list[str],
  X_test: list[str],
  y_test: list[str]):
  
  from sklearn.model_selection import StratifiedKFold, cross_validate
  from sklearn.metrics import accuracy_score, log_loss
  import numpy as np

  print("Model evaluations:")

  # Compute dummy log-loss baselines for log loss reduction calculations
  # probabilities for each class before seeing any data (how often each label appears in the dataset)
  priors = y_train.value_counts(normalize=True).reindex(fitted_estimator.classes_, fill_value=0).values
  # dummy estimator that uses class priors and ignores actual text
  baseline_proba = np.tile(priors, (len(y_test), 1))
  # baseline of how well dummy estimator predicted the true label
  ll_baseline = log_loss(y_test, baseline_proba, labels=fitted_estimator.classes_)

  # Compute cross-validations
  # These metrics help tune the model training params (algo, hyperparams, etc)
  # They also help to measure how balanced the training set is (labels + their descriptions)
  n_splits = 5
  cv = StratifiedKFold(n_splits=n_splits, shuffle=True, random_state=random_state)
  cv_scores = cross_validate(
    clone(pipeline),
    X_train,
    y_train,
    scoring={"accuracy": "accuracy", "log_loss": "neg_log_loss", "roc_auc": "roc_auc_ovr"},
    cv=cv,
    n_jobs=1,
    return_train_score=True,
    error_score="raise",
    verbose=0
  )

  cv_acc_mean = cv_scores["test_accuracy"].mean()
  cv_acc_std  = cv_scores["test_accuracy"].std()
  cv_ll_mean  = -cv_scores["test_log_loss"].mean()
  cv_ll_std   = cv_scores["test_log_loss"].std()
  cv_ll_reduction_mean = (ll_baseline - cv_ll_mean) / max(ll_baseline, 1e-12)
  cv_roc_auc_train_mean  = cv_scores["train_roc_auc"].mean()
  cv_roc_auc_train_std  = cv_scores["train_roc_auc"].std()
  cv_roc_auc_test_mean  = cv_scores["test_roc_auc"].mean()
  cv_roc_auc_test_std  = cv_scores["test_roc_auc"].std()
  
  # higher = better (1 - perfect)
  print(f"CV({n_splits}) Accuracy:    {cv_acc_mean:.4f} +/- {cv_acc_std:.4f}")
  # lower = better
  print(f"CV({n_splits}) Log Loss:    {cv_ll_mean:.4f} +/- {cv_ll_std:.4f}")
  # higher = better (1 - perfect)
  print(f"CV({n_splits}) Log Loss Reduction:    {cv_ll_reduction_mean:.4f} +/- {cv_ll_std:.4f}")
  # higher = better
  print(f"CV({n_splits}) ROC AUC Train:   {cv_roc_auc_train_mean:.4f} +/- {cv_roc_auc_train_std:.4f}")
  # higher = better
  print(f"CV({n_splits}) ROC AUC Test:     {cv_roc_auc_test_mean:.4f} +/- {cv_roc_auc_test_std:.4f}")
  # lower = better (higher difference suggests overfitting)
  print(f"CV({n_splits}) ROC AUC Train/Test delta:    {(cv_roc_auc_train_mean-cv_roc_auc_test_mean):4f}")

  # Compute hold-out metrics using test set
  # These metrics show how well this model will perform in the real world on previously unseen data
  y_pred = fitted_estimator.predict(X_test)
  hold_out_accuracy = accuracy_score(y_test, y_pred)

  y_proba = fitted_estimator.predict_proba(X_test)
  hold_out_ll = log_loss(y_test, y_proba, labels=fitted_estimator.classes_)

  # compare fitted estimator to baseline. 1 - perfect fit, 0 - no better than dummy
  hold_out_ll_reduction = (ll_baseline - hold_out_ll) / max(ll_baseline, 1e-12)

  # higher = better
  print(f"Holdout Accuracy: {hold_out_accuracy:.4f}")
  # lower = better
  print(f"Holdout Log Loss: {hold_out_ll:.4f}")
  # higher = better (1 - perfect)
  print(f"Holdout Log Loss Reduction: {hold_out_ll_reduction:.4f}")

  # Smaller the delta = better model and training set
  # Negative - CV looks too optimistic but real world performance may be less accurate
  # Positive - CV looks too pessimistic comparing to the real world performance. May need more training data.
  print(f"Holdout vs CV Accuracy delta: {(hold_out_accuracy - cv_acc_mean):.4f}")
  print(f"Holdout vs CV Log Loss Reduction delta: {(hold_out_ll_reduction - cv_ll_reduction_mean):.4f}")

def main():
  random_state = 500

  base_dir = Path(__file__).parent
  training_data_path = os.path.join(base_dir, "training_data", "plate_descriptions.json")

  json_data = read_raw_data(training_data_path)
  training_rows = transform_to_training_rows(json_data)
  X = training_rows["text"]
  y = training_rows["label"]
  X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.2, stratify=y, random_state=random_state
  )

  pipeline = create_pipeline(random_state=random_state)
  print("Fitting...")
  estimator = clone(pipeline).fit(X_train, y_train)
  print("Fitted successfully.")

  evaluate_model(pipeline, estimator, random_state, X_train, y_train, X_test, y_test)

  print_top_k("red top white middle blue bottom", estimator, 5)
  print_top_k("solid white plate", estimator, 5)
  print_top_k("green top", estimator, 5)
  print_top_k("blue white plate", estimator, 5)
  print_top_k("green plate", estimator, 5)

  # save to onnx
  onnx_export_path = os.path.join(base_dir, "..", "ui", "public", "skl_plates_model.onnx")
  export_to_onnx(estimator, onnx_export_path)

if __name__ == "__main__":
  main()