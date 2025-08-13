using Microsoft.ML.Data;
using Microsoft.ML.Transforms;

namespace PlateTrainer.Training.Models;

public sealed record TrainedModel(
  TransformerChain<TransformerChain<KeyToValueMappingTransformer>> Model,
  string[] Labels);
