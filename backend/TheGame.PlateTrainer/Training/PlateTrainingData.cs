namespace TheGame.PlateTrainer.Training;

public sealed record PlateTrainingData
{
  public string Key { get; set; } = default!;
  public Dictionary<string, string?> Description { get; set; } = [];
}

public sealed record PlateRow(string Label, string Text)
{
  public PlateRow() : this(string.Empty, string.Empty)
  { }
}