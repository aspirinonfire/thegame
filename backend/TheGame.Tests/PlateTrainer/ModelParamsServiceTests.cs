using TheGame.PlateTrainer;

namespace TheGame.Tests.PlateTrainer;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
public class ModelParamsServiceTests
{
  [Fact]
  public void WillBumpExistingVersion()
  {

    var actualNewVersion = ModelParamsService.BumpFileVersion("0.1.2");

    Assert.Equal("0.1.3", actualNewVersion);
  }

  [Fact]
  public void WillBumpMissingVersionWithDefault()
  { 
    var actualNewVersion = ModelParamsService.BumpFileVersion(null!);

    Assert.Equal("0.0.1", actualNewVersion);
  }

  [Fact]
  public void WillBumpEmptyVersionWithDefault()
  {
    var actualNewVersion = ModelParamsService.BumpFileVersion(string.Empty);

    Assert.Equal("0.0.1", actualNewVersion);
  }
}
