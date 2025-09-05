namespace TheGame.PlateTrainer.Training;

public sealed record PlateTrainingData
{
  public string Key { get; set; } = default!;
  public Dictionary<string, string?> Description { get; set; } = [];
}

public sealed record PlateTrainingRow(string Label, string Text)
{
  public PlateTrainingRow() : this(string.Empty, string.Empty)
  { }
}