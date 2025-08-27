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

class ModelEvaluations:
  cv_folds: int
  ndcg_k: int
  cv_acc_mean: float
  cv_acc_std: float
  cv_ll_mean: float
  cv_ll_std: float
  cv_roc_auc_train_mean: float
  cv_roc_auc_train_std: float
  cv_roc_auc_test_mean: float
  cv_roc_auc_test_std: float
  cv_roc_auc_train_test_delta: float
  cv_ndcg_train_mean: float
  cv_ndcg_train_std: float
  cv_ndcg_test_mean: float
  cv_ndcg_test_std: float
  cv_ncdg_train_test_delta: float
  holdout_acc: float
  holdout_ll: float
  holdout_vs_cv_acc_delta: float
  holdout_ndcg_score: float
  holdout_vs_cv_ncdg_delta: float

  def print(self):
    # higher = better (1 - perfect)
    cv_num = f"CV({self.cv_folds})"
    ncdg_num = f"NCDG({self.ndcg_k})"
    print(f"{cv_num} Accuracy:    {self.cv_acc_mean:.4f} +/- {self.cv_acc_std:.4f}")
    # lower = better
    print(f"{cv_num} Log Loss:    {self.cv_ll_mean:.4f} +/- {self.cv_ll_std:.4f}")
    # higher = better
    print(f"{cv_num} ROC AUC Train:   {self.cv_roc_auc_train_mean:.4f} +/- {self.cv_roc_auc_train_std:.4f}")
    # higher = better
    print(f"{cv_num} ROC AUC Test:     {self.cv_roc_auc_test_mean:.4f} +/- {self.cv_roc_auc_test_std:.4f}")
    # lower = better (higher difference suggests overfitting)
    print(f"{cv_num} ROC AUC Train/Test delta:    {(self.cv_roc_auc_train_mean-self.cv_roc_auc_test_mean):4f}")
    # higher = better
    print(f"{cv_num} {ncdg_num} Train:    {self.cv_ndcg_train_mean:.4f} +/- {self.cv_ndcg_train_std:.4f}")
    print(f"{cv_num} {ncdg_num} Test:    {self.cv_ndcg_test_mean:.4f} +/- {self.cv_ndcg_test_std:.4f}")
    # lower = better (higher difference suggests overfitting)
    print(f"{cv_num} NCDG Train/Test delta: {(self.cv_ndcg_train_mean-self.cv_ndcg_test_mean):.4f}")

    # higher = better
    print(f"Holdout {ncdg_num} {self.holdout_ndcg_score:.4f}")
    # lower = better
    print(f"Holdout vs CV {ncdg_num} {(self.holdout_ndcg_score-self.cv_ndcg_test_mean):.4f}")
    # higher = better
    print(f"Holdout Accuracy: {self.holdout_acc:.4f}")
    # lower = better
    print(f"Holdout Log Loss: {self.holdout_ll:.4f}")
    # Smaller the delta = better model and training set
    # Negative - CV looks too optimistic but real world performance may be less accurate
    # Positive - CV looks too pessimistic comparing to the real world performance. May need more training data.
    print(f"Holdout vs CV Accuracy delta: {(self.holdout_acc - self.cv_acc_mean):.4f}")


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
# and expanding vocabulary with synonyms and plate description keys
# Exanmple:
# Source JSON item:
# { "key": "us-ca", "description": { "plate" : "solid white" }}
# Denormalization/expanstion result:
# { "label": "us-ca", "text": "solid white" }
# { "label": "us-ca", "text": "solid white plate" }
# { "label": "us-ca", "text": "plate solid white" }
# { "label": "us-ca", "text": "all white" }
# { "label": "us-ca", "text": "all white plate" }
# { "label": "us-ca", "text": "plate all white" }
# { "label": "us-ca", "text": "solid white background" }
# { "label": "us-ca", "text": "background solid white" }
# { "label": "us-ca", "text": "all white background" }
# { "label": "us-ca", "text": "background all white" }
def transform_to_training_rows(training_data: list[RawTrainingDataRow]) -> pd.DataFrame:
  from itertools import product

  synonyms_lkp = {
    "middle": ["center"],
    "line": ["strip", "banner", "stripe"],
    "lines": ["strips", "banners", "stripes"],
    "solid": ["all"],
    "plate": ["background"]
  }

  print("Transforming to training rows...")
  
  training_row_df = pd.DataFrame(training_data)
  training_row_df = training_row_df[training_row_df["key"] != "sample"]

  # Generate training row text by expanding original training data with synonyms
  # and description keys.
  def create_training_text(plate_descriptions: dict[str, str]) -> list[str]:
    rows: list[str] = []

    for desc_key, desc_val in plate_descriptions.items():
      if not isinstance(desc_val, str):
        continue

      desk_key_variants = [desc_key, *synonyms_lkp.get(desc_key, [])]

      for raw_phrase in (p.strip() for p in desc_val.split(",") if p.strip()):
        phrase_variants_parts: list[list[str]] = []
        
        for word in raw_phrase.split(" "):
          word_synonyms = [word, *synonyms_lkp.get(word, [])]
          phrase_variants_parts.append(word_synonyms)
        
        for parts in product(*phrase_variants_parts):
          phrase = " ".join(parts)
          rows.append(phrase)

          for desk_key_variant in desk_key_variants:
            rows.append(f"{desk_key_variant} {phrase}")
            rows.append(f"{phrase} {desk_key_variant}")

    return rows

  training_rows = (
    training_row_df
      .rename(columns={"key": "label"})
      .assign(
        text = lambda frame: frame["description"].apply(create_training_text)
      )
      .explode("text", ignore_index=True)[["label", "text"]]
  )

  return training_rows

def create_vectorizer():
  from skl2onnx.sklapi import TraceableTfidfVectorizer # required for ONNX export!
  from skl2onnx import update_registered_converter
  from skl2onnx.shape_calculators.text_vectorizer import calculate_sklearn_text_vectorizer_output_shapes
  from skl2onnx.operator_converters.tfidf_vectoriser import convert_sklearn_tfidf_vectoriser

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

  return TraceableTfidfVectorizer(
    ngram_range=(1, 3),          # 1–4 n-grams
    lowercase=True,
    token_pattern=r"[a-z0-9-]+", # word tokenization (words only no spaces)
    stop_words=None,
    sublinear_tf=True)

# Train data set using Logistic Regression
# Tuned values as of 2025-08-26
# lbfgs:
# C=8, tol=0.004, max_iter=30
# ll-red: 0.4559, ROC AUC: .991 train/0.935 test
#
# saga:
# C=8, tol=0.01, max_iter=30
# ll-red: 0.4421, ROC AUC: .992 train/.934 test
#
# newton-cg:
# C=8, tol=0.001, max_iter=30
# ll-red: 0.4397, ROC AUC: .992 train/.934 test
def create_lr_pipeline(random_state: int = 42,
    C: int = 8,
    tol: float = 0.004,
    max_iter: int = 30) -> Pipeline:
  from sklearn.linear_model import LogisticRegression

  print("Building training Logistic Regression pipeline...")
  print(f"C={C}, tol={tol}, max_iter={max_iter}")

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

  return Pipeline(steps=[
    ("tfidf", create_vectorizer()),
    ("classifier", clf)
  ])

def create_svm_pipeline(random_state: int = 42,
    C: int = 8,
    cv: int = 5,
    tol: float = 0.004,
    max_iter: int = 30) -> Pipeline:
  from sklearn.svm import LinearSVC
  from sklearn.calibration import CalibratedClassifierCV

  print("Building training Linear SVC pipeline...")
  print(f"C={C}, tol={tol}, max_iter={max_iter}, cv={cv}")

  base = LinearSVC(
    C=C,
    loss="squared_hinge",
    penalty="l2",
    dual=False,
    tol=tol,
    # class_weight="balanced",
    max_iter=max_iter,
    multi_class="ovr",
    random_state=random_state,
    verbose=0,
  )

  clf = CalibratedClassifierCV(
    estimator=base,
    method="sigmoid",
    cv=cv,
    n_jobs=1,
    ensemble=True,
  )

  return Pipeline(steps=[
    ("tfidf", create_vectorizer()),
    ("classifier", clf)
  ])

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

def get_feature_contribs(pipeline: Pipeline, class_labels: list[str], query: str):
  import numpy as np
  
  vec = pipeline.named_steps["tfidf"]
  clf = pipeline.named_steps["classifier"]

  query_vector = vec.transform([query])
  active_token_indices = query_vector.nonzero()[1]
  tfidf_values = query_vector.data
  active_tokens = vec.get_feature_names_out()[active_token_indices]
  
  classes = clf.classes_
  coefs = clf.coef_  # shape [n_classes, n_features]
  intercept = getattr(clf, "intercept_", np.zeros(coefs.shape[0]))

  print(f"Activated tokens: {active_tokens}")
  idfs = dict(zip(vec.get_feature_names_out(), vec.idf_))
  for active_token in active_tokens:
    print(f"{active_token}: {idfs[active_token]}")

  for class_label in class_labels:
    class_idx = np.where(classes == class_label)[0][0]
    class_coefs = coefs[class_idx, active_token_indices]
    contributions = tfidf_values * class_coefs
    total_score = float(contributions.sum() + intercept[class_idx])

    print(f"{class_label} (total {total_score:.4f}):")
    for token, token_weight_for_class in zip(active_tokens, class_coefs):
      print(f"  {token}: {token_weight_for_class:.4f}")

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

def compute_model_evaluations(pipeline: Pipeline,
  fitted_estimator: Pipeline,
  random_state: int,
  X_train: list[str],
  y_train: list[str],
  X_test: list[str],
  y_test: list[str]) -> ModelEvaluations:
  
  from sklearn.model_selection import StratifiedKFold, cross_validate
  from sklearn.metrics import accuracy_score, log_loss
  from sklearn.naive_bayes import LabelBinarizer
  from sklearn.metrics import ndcg_score, get_scorer_names

  print("Model evaluations:")
  evals = ModelEvaluations()
  
  lb = LabelBinarizer()

  def make_ndcg_scorer(k=10):
    def _scorer(estimator, X, y):
        proba = estimator.predict_proba(X)
        lb.fit(estimator.classes_)
        y_true_bin = lb.transform(y)
        return float(ndcg_score(y_true_bin, proba, k=k))
    return _scorer

  ndcg_k = 10
  ndcg_at_k = make_ndcg_scorer(ndcg_k)

  # Compute cross-validations
  # These metrics help tune the model training params (algo, hyperparams, etc)
  # They also help to measure how balanced the training set is (labels + their descriptions)
  n_splits = 5
  cv = StratifiedKFold(n_splits=n_splits, shuffle=True, random_state=random_state)
  cv_scores = cross_validate(
    clone(pipeline),
    X_train,
    y_train,
    scoring={
      "accuracy": "accuracy",
      "log_loss": "neg_log_loss",
      "roc_auc": "roc_auc_ovr",
      "ncdg": ndcg_at_k
    },
    cv=cv,
    n_jobs=1,
    return_train_score=True,
    error_score="raise",
    verbose=0
  )

  evals.cv_folds = n_splits
  evals.ndcg_k = ndcg_k
  evals.cv_acc_mean = cv_scores["test_accuracy"].mean()
  evals.cv_acc_std  = cv_scores["test_accuracy"].std()
  evals.cv_ll_mean  = -cv_scores["test_log_loss"].mean()
  evals.cv_ll_std   = cv_scores["test_log_loss"].std()
  evals.cv_roc_auc_train_mean  = cv_scores["train_roc_auc"].mean()
  evals.cv_roc_auc_train_std  = cv_scores["train_roc_auc"].std()
  evals.cv_roc_auc_test_mean  = cv_scores["test_roc_auc"].mean()
  evals.cv_roc_auc_test_std  = cv_scores["test_roc_auc"].std()
  evals.cv_ndcg_train_mean = cv_scores["train_ncdg"].mean()
  evals.cv_ndcg_train_std = cv_scores["train_ncdg"].std()
  evals.cv_ndcg_test_mean = cv_scores["test_ncdg"].mean()
  evals.cv_ndcg_test_std = cv_scores["test_ncdg"].std()

  # Compute hold-out metrics using test set
  # These metrics show how well this model will perform in the real world on previously unseen data
  y_pred = fitted_estimator.predict(X_test)
  evals.holdout_acc = accuracy_score(y_test, y_pred)

  y_proba = fitted_estimator.predict_proba(X_test)
  y_test_bin = lb.fit(fitted_estimator.classes_).transform(y_test)
  evals.holdout_ndcg_score = ndcg_score(y_test_bin, y_proba, k=ndcg_k)
  evals.holdout_ll = log_loss(y_test, y_proba, labels=fitted_estimator.classes_)

  return evals

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
  onnx_export_path = os.path.join(base_dir, "..", "ui", "public", "skl_plates_model.onnx")
  export_to_onnx(lr_estimator, onnx_export_path)

if __name__ == "__main__":
  main()