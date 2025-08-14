using Microsoft.ML.Data;

namespace PlateTrainer.Training;

public sealed record PlatePrediction
{
  [ColumnName("PredictedLabel")]
  public string PredictedLabel { get; set; } = default!;

  // keep scores if you ever want top-k
  [ColumnName("Score")]
  public float[] Scores { get; set; } = [];
}