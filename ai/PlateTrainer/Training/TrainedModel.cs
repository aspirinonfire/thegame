using Microsoft.ML.Data;
using Microsoft.ML.Transforms;

namespace PlateTrainer.Training;

public sealed record TrainedModel(TransformerChain<TransformerChain<KeyToValueMappingTransformer>> Model, string[] Labels);
