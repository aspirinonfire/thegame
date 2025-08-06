using Microsoft.ML;
using Microsoft.ML.Transforms.Text;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using PlateTrainer.Prediction;

namespace PlateTrainer.Training;

public sealed class PlateModelTrainerPipelineFactory
{
  public EstimatorChain<TransformerChain<KeyToValueMappingTransformer>> GetMlPipeline(MLContext ml)
  {
    Console.WriteLine("----- Creating training pipeline...");

    var trainingPipeline = ml.Transforms.Text
      .FeaturizeText(
        outputColumnName: "Features",
        new TextFeaturizingEstimator.Options()
        {
          WordFeatureExtractor = new()
          {
            NgramLength = 2,
            UseAllLengths = true
          },
          CharFeatureExtractor = null,  // we care about words only for now
          KeepPunctuations = false,
          KeepNumbers = true,
        },
        inputColumnNames: nameof(PlateTrainingRow.Text))
      .Append(ml.Transforms.Conversion.MapValueToKey(nameof(PlateTrainingRow.Label), nameof(PlateTrainingRow.Label))
        .Append(ml.MulticlassClassification.Trainers.SdcaMaximumEntropy(
          labelColumnName: nameof(PlateTrainingRow.Label),
          featureColumnName: "Features",
          exampleWeightColumnName: nameof(PlateTrainingRow.Weight),
          maximumNumberOfIterations: 100
        ))
        .Append(ml.Transforms.Conversion.MapKeyToValue(nameof(PlatePrediction.PredictedLabel), nameof(PlatePrediction.PredictedLabel))));

    return trainingPipeline;
  }
}
