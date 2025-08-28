from sklearn import clone
from sklearn.model_selection import StratifiedKFold, cross_validate
from sklearn.metrics import accuracy_score, log_loss
from sklearn.naive_bayes import LabelBinarizer
from sklearn.metrics import ndcg_score
from sklearn.pipeline import Pipeline

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

def compute_model_evaluations(pipeline: Pipeline,
  fitted_estimator: Pipeline,
  random_state: int,
  X_train: list[str],
  y_train: list[str],
  X_test: list[str],
  y_test: list[str]) -> ModelEvaluations:

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