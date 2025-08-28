from skl2onnx.sklapi import TraceableTfidfVectorizer # required for ONNX export!
from skl2onnx import update_registered_converter
from skl2onnx.shape_calculators.text_vectorizer import calculate_sklearn_text_vectorizer_output_shapes
from skl2onnx.operator_converters.tfidf_vectoriser import convert_sklearn_tfidf_vectoriser
from sklearn.linear_model import LogisticRegression
from sklearn.pipeline import Pipeline
from sklearn.svm import LinearSVC
from sklearn.calibration import CalibratedClassifierCV

VEC_STEP = "vec"
CLF_STEP = "clf"

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
    lowercase=True,
    token_pattern=r"[a-z0-9-]+", # word tokenization (words only no spaces)
    stop_words=None,
    sublinear_tf=True)

# Train data set using Logistic Regression
def create_lr_pipeline(random_state: int = 42, max_iter: int = 500) -> Pipeline:
  # see https://scikit-learn.org/stable/modules/generated/sklearn.linear_model.LogisticRegression.html
  clf = LogisticRegression(
    solver="lbfgs",
    penalty="l2",
    max_iter=max_iter,
    random_state=random_state,
    n_jobs=-1)

  return Pipeline(steps=[
    (VEC_STEP, create_vectorizer()),
    (CLF_STEP, clf)
  ])

def create_svm_pipeline(random_state: int = 42) -> Pipeline:
  clf = CalibratedClassifierCV(
    estimator=LinearSVC(
      loss="squared_hinge",
      penalty="l2",
      multi_class="ovr",
      random_state=random_state,
    ),
    n_jobs=-1,
    ensemble=True,
  )

  return Pipeline(steps=[
    (VEC_STEP, create_vectorizer()),
    (CLF_STEP, clf)
  ])

