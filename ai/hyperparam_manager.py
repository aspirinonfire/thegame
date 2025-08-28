from dataclasses import asdict, dataclass, is_dataclass
from pathlib import Path
from sklearn.model_selection import GridSearchCV
from sklearn.pipeline import Pipeline
from typing import Any, Dict, List, Literal, TypeAlias
import json

from evaluator import ModelEvaluations, compute_model_evaluations
from model_utils import make_ndcg_scorer

@dataclass
class ModelParams:
  refit: str
  best_params: Dict
  final_evals: ModelEvaluations

@dataclass
class SearchResults:
  best_estimator: Pipeline
  best_score: float
  estimator_params: Dict
  model_evals: ModelEvaluations
  refit: str

  def print_results(self):
    print(f"Best params:\n{self.estimator_params}")
    print(f"Best score: {self.best_score:.4f}")
    self.model_evals.print()
  
  def to_model_params(self):
    return ModelParams(best_params = self.estimator_params,
      final_evals = self.model_evals,
      refit=self.refit)

Refit: TypeAlias = Literal["neg_log_loss", "ndcg"]

def create_hyperparams_search(estimator_pipeline: Pipeline,
  param_grid: Dict[str, List[Any]],
  refit: TypeAlias):
  
  scoring = {
    "neg_log_loss": "neg_log_loss",
    "accuracy": "accuracy",
    "ndcg": make_ndcg_scorer(k=10)
  }

  return GridSearchCV(
    estimator=estimator_pipeline,
    param_grid=param_grid,
    scoring=scoring,
    refit=refit,
    cv=5,
    verbose=2,
    return_train_score=False,
    n_jobs=-1
  )

def find_best_lr_params(pipeline: Pipeline,
  X_train: Any,
  X_test: Any,
  y_train: Any,
  y_test: Any,
  random_state: int,
  param_grid: Dict[str, List[Any]],
  refit: Refit) -> SearchResults:

  search = create_hyperparams_search(pipeline, param_grid, refit=refit)

  search.fit(X_train, y_train)

  model_evals = compute_model_evaluations(pipeline,
    search.best_estimator_,
    random_state,
    X_train,
    y_train,
    X_test,
    y_test)
  
  return SearchResults(best_estimator=search.best_estimator_,
    best_score=search.best_score_,
    estimator_params=search.best_params_,
    model_evals=model_evals,
    refit=refit)


def save_hyperparams_to_file(filepath:str, params: ModelParams):
  with open(filepath, "w") as f:
    json.dump(asdict(params), fp=f, indent=2, ensure_ascii=False)


def load_hyperparams_from_file(filepath: str) -> Dict[str, List[Any]]:
  hyperparam_file = Path(filepath)

  with hyperparam_file.open("r", encoding="utf-8") as file:
    config: ModelParams = json.load(file)

  training_params = config["best_params"]
  training_params["vec__ngram_range"] = tuple(training_params["vec__ngram_range"])
  return training_params