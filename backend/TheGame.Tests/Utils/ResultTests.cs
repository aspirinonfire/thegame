namespace TheGame.Tests.Utils;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
public class ResultTests
{
  [Fact]
  public void WillReturnSuccessfulResultOnSuccessfulMaybe()
  {
    Result<int> uutResult = 1;

    AssertResult.AssertIsSucceessful(uutResult,
      actualSuccessfulValue => Assert.Equal(1, actualSuccessfulValue));
  }

  [Fact]
  public void WillReturnSuccessfulResultOnNullableSuccessfulMaybe()
  {
    Result<int?> uutResult = (int?)null;

    AssertResult.AssertIsSucceessful(uutResult,
      actualSuccessfulValue => Assert.Null(actualSuccessfulValue));
  }

  [Fact]
  public void WillReturnFailureResultOnFailureMaybe()
  {
    Result<int> uutResult = new Failure("test");

    AssertResult.AssertIsFailure(uutResult,
      actualFailureValue => Assert.Equal("test", actualFailureValue.ErrorMessage));
  }
}
