from dataclasses import dataclass
import pandas
from sklearn.pipeline import Pipeline
import os;
from pathlib import Path

@dataclass
class RawTrainingDataRow:
  key: str
  version: str
  weight: float
  description: dict[str, str]

# Read training data json and transform it into de-normalized training rows
# Note: we are loading entire data set into memory because it is small enough and makes rest of the code simpler
# For larger data sets, we'll need to refactor solution to work with streams, and de-normalize once
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
def transform_to_training_rows(training_data: list[RawTrainingDataRow]) -> pandas.DataFrame:
  print("Transforming to training rows...")
  training_row_df = pandas.DataFrame(training_data)
  return (
    training_row_df
      .rename(columns={"key": "label"})
      .assign(
        text = lambda frame: frame["description"]
          .apply(lambda mapping: [
            f"{phrase.strip()} {desc_key}"
            for desc_key, desc_val in mapping.items()
            for phrase in (desc_val.split(",") if isinstance(desc_val, str) else [])
            if phrase.strip()
          ]
        )
      )
      .explode("text", ignore_index=True)[["label", "text"]]
  )

# Train data set using logistic regression (MaxEnt) with tfidf tokenization and saga optimizer
def train_maxent(training_rows: pandas.DataFrame,
                 random_state: int = 42,
                 l2_strength: int = 10,
                 max_iter: int = 50) -> Pipeline:
  from skl2onnx.sklapi import TraceableTfidfVectorizer # required for ONNX export!
  from skl2onnx import update_registered_converter
  from skl2onnx.shape_calculators.text_vectorizer import (
      calculate_sklearn_text_vectorizer_output_shapes,
  )
  from skl2onnx.operator_converters.tfidf_vectoriser import convert_sklearn_tfidf_vectoriser
  from sklearn.linear_model import LogisticRegression

  print("Training...")

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

  pipeline = Pipeline(steps=[
    ("tfidf", TraceableTfidfVectorizer(
      ngram_range=(1, 4),          # 1–4 n-grams
      lowercase=True,
      token_pattern=r"[a-z0-9-]+", # word tokenization (words only no spaces)
      min_df=1,
      stop_words=None
  )),
    ("clf", LogisticRegression(
      penalty="l2",
      C=l2_strength,
      solver="saga",
      max_iter=max_iter,
      random_state=random_state,
      n_jobs=-1
    ))
  ])

  trained_model = pipeline.fit(X=training_rows["text"].astype(str),
                               y=training_rows["label"].astype(str))
  print("Model trained.")
  return trained_model

# Print query predictions against trained model
def print_top_k(query_text: str, trained_model: Pipeline, top_k: int = 5):
  labels = trained_model.named_steps["clf"].classes_

  raw_probs = trained_model.predict_proba([query_text])[0]
  
  probabilities = (
    pandas.DataFrame({"label": labels, "probability": raw_probs})
      .sort_values("probability", ascending=False)
      .reset_index(drop=True)
  )

  print(f"== \"{query_text}\" ==")
  print(probabilities.head(top_k))

def export_to_onnx(trained_model: Pipeline, export_path: str):
  from skl2onnx import to_onnx
  from skl2onnx.common.data_types import StringTensorType

  print("Exporting to ONNX...")

  onnx = to_onnx(
    trained_model,
    initial_types=[("text", StringTensorType([None, 1]))],  # batch of strings [N,1]
    options={
      id(trained_model.named_steps["clf"]): {"zipmap": False, "output_class_labels": True}
    }
  )

  with open(export_path, "wb") as f:
    f.write(onnx.SerializeToString())

  print("Exported to ONNX successfully")

# main script
base_dir = Path(__file__).parent
training_data_path = os.path.join(base_dir, "training_data", "plate_descriptions.json")

json_data = read_raw_data(training_data_path)
training_rows = transform_to_training_rows(json_data)
trained_model = train_maxent(training_rows)

# TODO evaluate accuracy, log-loss, cv k-fold, etc

print_top_k("red top white middle blue bottom", trained_model, 5)
print_top_k("solid white plate", trained_model, 5)
print_top_k("green top", trained_model, 5)

# save to onnx
onnx_export_path = os.path.join(base_dir, "..", "ui", "public", "skl_plates_model.onnx")
export_to_onnx(trained_model, onnx_export_path)
