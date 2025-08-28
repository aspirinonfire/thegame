from skl2onnx.sklapi import TraceableTfidfVectorizer # required for ONNX export!
from skl2onnx import update_registered_converter
from skl2onnx.shape_calculators.text_vectorizer import calculate_sklearn_text_vectorizer_output_shapes
from skl2onnx.operator_converters.tfidf_vectoriser import convert_sklearn_tfidf_vectoriser
from sklearn.linear_model import LogisticRegression
from sklearn.pipeline import Pipeline
from sklearn.svm import LinearSVC
from sklearn.calibration import CalibratedClassifierCV

def create_vectorizer():
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
def create_lr_pipeline(random_state: int = 42,
    C: int = 8,
    tol: float = 0.004,
    max_iter: int = 30) -> Pipeline:

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