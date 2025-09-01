import unittest
import sys, os
import json

from evaluator import ModelEvaluations
from hyperparam_manager import SearchResults, bump_version
from pipeline_factory import CLF_STEP, create_lr_pipeline, create_svm_pipeline
sys.path.append(os.path.dirname(os.path.dirname(__file__)))

class TestHyperparamManager(unittest.TestCase):
  def test_will_extract_estimator_name_for_lr(self):
    lr_pipeline = create_lr_pipeline()

    uut_search_result = SearchResults(best_estimator=lr_pipeline,
      best_score=0.0,
      estimator_params={"test":"test"},
      model_evals=ModelEvaluations,
      refit="test")
    
    actual_model_params = uut_search_result.to_model_params()

    self.assertEqual("LogisticRegression", actual_model_params.estimator)

  def test_will_extract_estimator_name_for_svm(self):
    svm_pipeline = create_svm_pipeline()

    uut_search_result = SearchResults(best_estimator=svm_pipeline,
      best_score=0.0,
      estimator_params={"test":"test"},
      model_evals=ModelEvaluations,
      refit="test")
    
    actual_model_params = uut_search_result.to_model_params()

    self.assertEqual("SVC", actual_model_params.estimator)

  def test_will_create_default_version(self):
    test_json = json.loads('{}')
    old_version = test_json.get("version")
    actual_new_version = bump_version(old_version)

    self.assertEqual("0.0.1", actual_new_version)

  def test_will_increment_version(self):
    test_json = json.loads('{"version": "0.0.1"}')
    old_version = test_json.get("version")
    actual_new_version = bump_version(old_version)

    self.assertEqual("0.0.2", actual_new_version)
