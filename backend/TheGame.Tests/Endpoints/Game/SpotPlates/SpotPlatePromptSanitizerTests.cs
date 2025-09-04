using TheGame.Api.Endpoints.Game.SpotPlates;

namespace TheGame.Tests.Endpoints.Game.SpotPlates;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
public class SpotPlatePromptSanitizerTests
{
  [Fact]
  public void NormalizePrompt_WhenCleanInput_ReturnsSame()
  {
    var input = "find blue sedan with mountain backdrop";
    var actual = SpotLicensePlatesCommandHandler.NormalizePrompt(input);
    Assert.Equal(input, actual);
  }

  [Fact]
  public void NormalizePrompt_RemovesInvalidControlCharacters()
  {
    var input = " \u0000Hello\u0007 World\t ";
    var expected = "Hello World";
    var actual = SpotLicensePlatesCommandHandler.NormalizePrompt(input);
    Assert.Equal(expected, actual);
  }

  [Fact]
  public void NormalizePrompt_TruncatesOverlongInput()
  {
    var input = new string('A', 3000);
    var actual = SpotLicensePlatesCommandHandler.NormalizePrompt(input);
    Assert.Equal(2048, actual.Length);
    Assert.StartsWith(new string('A', 100), actual);
  }
}
