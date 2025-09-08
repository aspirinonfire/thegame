using Microsoft.ML.Data;

namespace TheGame.PlateTrainer.Prediction;

public sealed record PlatePrediction
{
  [ColumnName("PredictedLabel")]
  public uint PredictedLabel { get; set; }

  // keep scores if you ever want top-k
  [ColumnName("Score")]
  public float[] Scores { get; set; } = [];
}