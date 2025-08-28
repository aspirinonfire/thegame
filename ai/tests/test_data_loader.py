import unittest
import sys, os
sys.path.append(os.path.dirname(os.path.dirname(__file__)))

from data_loader import RawTrainingDataRow, transform_to_training_rows

class TestDataLoaderMethods(unittest.TestCase):
  def test_will_produce_correct_row_columns(self):
    test_data = RawTrainingDataRow(key="test-key",
      version="test-ver",
      weight=0.0,
      description={
        "test_category": "test phrase"
      })
    
    uut_rows = transform_to_training_rows([test_data])

    self.assertEqual(["label", "text"], [*uut_rows])
  
  def test_will_produce_denormalized_rows_from_one_phrase(self):
    test_data = RawTrainingDataRow(key="test-key",
      version="test-ver",
      weight=0.0,
      description={
        "test_category": "test phrase"
      })
    
    uut_texts = transform_to_training_rows([test_data])["text"].tolist()

    self.assertEqual(
      ["test phrase", "test_category test phrase", "test phrase test_category"],
      uut_texts)
    
  def test_will_produce_rows_from_two_phrases(self):
    test_data = RawTrainingDataRow(key="test-key",
      version="test-ver",
      weight=0.0,
      description={
        "test_category": "phrase 1, phrase 2"
      })
    
    uut_texts = transform_to_training_rows([test_data])["text"].tolist()

    self.assertEqual(
      ["phrase 1", "test_category phrase 1", "phrase 1 test_category", "phrase 2", "test_category phrase 2", "phrase 2 test_category"],
      uut_texts)
    
  def test_will_produce_synonyms(self):
    test_data = RawTrainingDataRow(key="test-key",
      version="test-ver",
      weight=0.0,
      description={
        "middle": "test"
      })
    
    uut_texts = transform_to_training_rows([test_data])["text"].tolist()

    self.assertIn("middle test", uut_texts)
    self.assertIn("center test", uut_texts)

if __name__ == '__main__':
  unittest.main()