namespace PlateTrainer.Training.Models;

public sealed record PlateTrainingData
{
  public string Key { get; set; } = default!;
  public Description[] Phrases { get; set; } = [];

  public sealed record Description
  {
    public string Text { get; set; } = default!;
    public float Weight { get; set; }
  }
}

public sealed record PlateTrainingRow(string Label, string Text, float Weight)
{
  public PlateTrainingRow() : this(string.Empty, string.Empty, 0)
  { }
}
