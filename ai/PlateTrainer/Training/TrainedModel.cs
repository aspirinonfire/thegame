using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using System.Collections.Immutable;

namespace PlateTrainer.Training;

public sealed record TrainedModel(TransformerChain<TransformerChain<KeyToValueMappingTransformer>> Model,
  string[] Labels);
