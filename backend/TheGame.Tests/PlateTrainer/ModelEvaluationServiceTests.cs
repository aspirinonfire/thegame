using TheGame.PlateTrainer;

namespace TheGame.Tests.PlateTrainer;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
public class ModelEvaluationServiceTests
{
  [Fact]
  public void WillCalculateNdcgForFirstPlaceRow()
  {
    var rows = new CvFoldScores[]
    {
      // expected label has the highest predicted probability
      new() { Label = 1, Score = [0.90f, 0.10f, 0.06f, 0.04f] },
    };

    var ndcg = ModelEvaluationService.CalculateNdcg(rows, k: 4);

    Assert.Equal(1.000, ndcg, precision: 3);
  }

  [Fact]
  public void WillCalculateNdcgForLastPlaceRow()
  {
    var rows = new CvFoldScores[]
    {
      // expected label has the lowest predicted probability
      new() { Label = 1, Score = [0.00f, 0.35f, 0.35f, 0.30f] },
    };

    var ndcg = ModelEvaluationService.CalculateNdcg(rows, k: 4);

    Assert.Equal(0.431, ndcg, precision: 3);
  }

  [Fact]
  public void WillCalculateNdcgForSecondPlaceRow()
  {
    var rows = new CvFoldScores[]
    {
      // expected label has 2nd highest probability
      new() { Label = 1, Score = [0.30f, 0.35f, 0.25f, 0.15f] },
    };

    var ndcg = ModelEvaluationService.CalculateNdcg(rows, k: 4);

    Assert.Equal(0.631, ndcg, precision: 3);
  }

  [Fact]
  public void WillCalculateAverageNdcgForPerfectAndSecondPlaceRows()
  {
    var rows = new CvFoldScores[]
    {
      // expected label has the highest predicted probability
      new() { Label = 1, Score = [0.90f, 0.10f, 0.06f, 0.04f] },
      // expected label has 2nd highest probability
      new() { Label = 2, Score = [0.35f, 0.30f, 0.25f, 0.15f] },
    };

    var ndcg = ModelEvaluationService.CalculateNdcg(rows, k: 4);

    Assert.Equal(0.815, ndcg, precision: 3);
  }
}
