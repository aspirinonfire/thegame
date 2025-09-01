from dataclasses import dataclass
from itertools import product
import pandas as pd
from pathlib import Path
import json;

@dataclass
class RawTrainingDataRow:
  key: str
  version: str
  weight: float
  description: dict[str, str]

synonyms_lkp = {
  "middle": ["center"],
  "line": ["strip", "banner", "stripe"],
  "lines": ["strips", "banners", "stripes"],
  "solid": ["all"],
  "plate": ["background"]
}

# Read training data json and transform it into de-normalized training rows.
# Note: we are loading entire data set into memory because it is small enough and makes rest of the code simpler.
# For larger data sets, we'll need to refactor solution to work with streams, and de-normalize once into a temp file.
def read_raw_data(training_data_path: str) -> list[RawTrainingDataRow]:
  
  print(f"Reading raw json data from {training_data_path}...")

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
  print("Transforming to training rows...")
  
  training_row_df = pd.DataFrame(training_data)
  training_row_df = training_row_df[training_row_df["key"] != "sample"]

  training_rows = (
    training_row_df
      .rename(columns={"key": "label"})
      .assign(
        text = lambda frame: frame["description"].apply(create_training_text)
      )
      .explode("text", ignore_index=True)[["label", "text"]]
  )

  return training_rows

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