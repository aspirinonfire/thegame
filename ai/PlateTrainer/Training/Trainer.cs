using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using System.Data;

namespace PlateTrainer.Training;

public sealed class Trainer
{
  public TrainedModel Train(EstimatorChain<TransformerChain<KeyToValueMappingTransformer>> pipeline,
    DataOperationsCatalog.TrainTestData data,
    DataViewSchema dataSchema)
  {
    Console.WriteLine("Training...");
    var trainedModel = pipeline.Fit(data.TrainSet);

    // map key indices -> label strings taken from the *Label* column
    var outputSchema = trainedModel.GetOutputSchema(dataSchema);
    var labelCol = outputSchema[nameof(PlateTrainingRow.Label)];
    var keyBuffer = default(VBuffer<ReadOnlyMemory<char>>);
    labelCol.GetKeyValues(ref keyBuffer);
    
    var labels = keyBuffer.DenseValues()
      .Select(x => x.ToString())
      .ToArray();

    return new TrainedModel(trainedModel, labels);
  }
}
