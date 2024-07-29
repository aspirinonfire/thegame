using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Tests.DomainModels.Games;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
public class GameScoreCalculatorTests
{
  [Fact]
  public void WillCalculateZeroWhenNothingIsSpotted()
  {
    var spottedPlates = Array.Empty<LicensePlate.PlateKey>();

    var uutCalculator = new GameScoreCalculator();

    var actualScore = uutCalculator.CalculateGameScore(spottedPlates);

    Assert.Equal(0, actualScore.NumberOfSpottedPlates);
    Assert.Equal(0, actualScore.TotalScore);
    Assert.Empty(actualScore.Achievements);
  }

  [Fact]
  public void WillCalculateScoresWithoutAchievements()
  {
    var spottedPlates = new LicensePlate.PlateKey[]
    {
      new(Country.US, StateOrProvince.CA),
      new(Country.US, StateOrProvince.OR),
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
    var spottedPlates = new LicensePlate.PlateKey[]
    {
      new (Country.US, StateOrProvince.CA),
      new (Country.US, StateOrProvince.OR),
      new (Country.US, StateOrProvince.WA),
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
    var spottedPlates = new LicensePlate.PlateKey[]
    {
      new (Country.US, StateOrProvince.CT),
      new (Country.US, StateOrProvince.DE),
      new (Country.US, StateOrProvince.FL),
      new (Country.US, StateOrProvince.GA),
      new (Country.US, StateOrProvince.ME),
      new (Country.US, StateOrProvince.MD),
      new (Country.US, StateOrProvince.MA),
      new (Country.US, StateOrProvince.NH),
      new (Country.US, StateOrProvince.NJ),
      new (Country.US, StateOrProvince.NY),
      new (Country.US, StateOrProvince.NC),
      new (Country.US, StateOrProvince.RI),
      new (Country.US, StateOrProvince.SC),
      new (Country.US, StateOrProvince.VA)
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
    var spottedPlates = new LicensePlate.PlateKey[]
    {
      new (Country.US, StateOrProvince.CA),
      new (Country.US, StateOrProvince.AZ),
      new (Country.US, StateOrProvince.NM),
      new (Country.US, StateOrProvince.TX),
      new (Country.US, StateOrProvince.AR),
      new (Country.US, StateOrProvince.TN),
      new (Country.US, StateOrProvince.NC)
    };
    
    var uutCalculator = new GameScoreCalculator();

    var actualScore = uutCalculator.CalculateGameScore(spottedPlates);

    Assert.Equal(7, actualScore.NumberOfSpottedPlates);
    Assert.Equal(107, actualScore.TotalScore);
    
    var actualAchievement = Assert.Single(actualScore.Achievements);
    Assert.Equal("Coast-to-Coast", actualAchievement);
  }
}
