using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Tests.DomainModels.Games
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
  public class GameScoreCalculatorTests
  {
    [Fact]
    public void WillCalculateZeroWhenNothingIsSpotted()
    {
      var spottedPlates = Array.Empty<(Country, StateOrProvince)>();

      var uutCalculator = new GameScoreCalculator();

      var actualScore = uutCalculator.CalculateGameScore(spottedPlates);

      Assert.Equal(0, actualScore.NumberOfSpottedPlates);
      Assert.Equal(0, actualScore.TotalScore);
      Assert.Empty(actualScore.Achievements);
    }

    [Fact]
    public void WillCalculateScoresWithoutAchievements()
    {
      var spottedPlates = new[]
      {
        (Country.US, StateOrProvince.CA),
        (Country.US, StateOrProvince.OR),
      };

      var uutCalculator = new GameScoreCalculator();

      var actualScore = uutCalculator.CalculateGameScore(spottedPlates);
      
      Assert.Equal(2, actualScore.NumberOfSpottedPlates);
      Assert.Equal(2, actualScore.TotalScore);
      Assert.Empty(actualScore.Achievements);
    }

    [Fact]
    public void WillIncludeWestCoastBonusWhenAchieved()
    {
      var spottedPlates = new[]
      {
        (Country.US, StateOrProvince.CA),
        (Country.US, StateOrProvince.OR),
        (Country.US, StateOrProvince.WA),
      };

      var uutCalculator = new GameScoreCalculator();

      var actualScore = uutCalculator.CalculateGameScore(spottedPlates);

      Assert.Equal(3, actualScore.NumberOfSpottedPlates);
      Assert.Equal(13, actualScore.TotalScore);
      
      var actualAchievement = Assert.Single(actualScore.Achievements);
      Assert.Equal("West Coast", actualAchievement);
    }

    [Fact]
    public void WillIncludeEastCoastBonusWhenAchieved()
    {
      var spottedPlates = new[]
      {
        (Country.US, StateOrProvince.CT),
        (Country.US, StateOrProvince.DE),
        (Country.US, StateOrProvince.FL),
        (Country.US, StateOrProvince.GA),
        (Country.US, StateOrProvince.ME),
        (Country.US, StateOrProvince.MD),
        (Country.US, StateOrProvince.MA),
        (Country.US, StateOrProvince.NH),
        (Country.US, StateOrProvince.NJ),
        (Country.US, StateOrProvince.NY),
        (Country.US, StateOrProvince.NC),
        (Country.US, StateOrProvince.RI),
        (Country.US, StateOrProvince.SC),
        (Country.US, StateOrProvince.VA)
      };

      var uutCalculator = new GameScoreCalculator();

      var actualScore = uutCalculator.CalculateGameScore(spottedPlates);

      Assert.Equal(14, actualScore.NumberOfSpottedPlates);
      Assert.Equal(44, actualScore.TotalScore);
      
      var actualAchievement = Assert.Single(actualScore.Achievements);
      Assert.Equal("East Coast", actualAchievement);
    }

    [Fact]
    public void WillIncludeCoastToCoastWhenAchieved()
    {
      var spottedPlates = new[]
      {
        (Country.US, StateOrProvince.CA),
        (Country.US, StateOrProvince.AZ),
        (Country.US, StateOrProvince.NM),
        (Country.US, StateOrProvince.TX),
        (Country.US, StateOrProvince.AR),
        (Country.US, StateOrProvince.TN),
        (Country.US, StateOrProvince.NC)
      };
      
      var uutCalculator = new GameScoreCalculator();

      var actualScore = uutCalculator.CalculateGameScore(spottedPlates);

      Assert.Equal(7, actualScore.NumberOfSpottedPlates);
      Assert.Equal(107, actualScore.TotalScore);
      
      var actualAchievement = Assert.Single(actualScore.Achievements);
      Assert.Equal("Coast-to-Coast", actualAchievement);
    }
  }
}
