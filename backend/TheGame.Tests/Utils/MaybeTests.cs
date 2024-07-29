namespace TheGame.Tests.Utils;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
public class MaybeTests
{
  [Fact]
  public void WillReturnSuccessfulResultOnSuccessfulMaybe()
  {
    Maybe<int> uutMaybe = 1;

    AssertMaybe.AssertIsSucceessful(uutMaybe,
      actualSuccessfulValue => Assert.Equal(1, actualSuccessfulValue));
  }

  [Fact]
  public void WillReturnSuccessfulResultOnNullableSuccessfulMaybe()
  {
    Maybe<int?> uutMaybe = (int?)null;

    AssertMaybe.AssertIsSucceessful(uutMaybe,
      actualSuccessfulValue => Assert.Null(actualSuccessfulValue));
  }

  [Fact]
  public void WillReturnFailureResultOnFailureMaybe()
  {
    Maybe<int> uutMaybe = new Failure("test");

    AssertMaybe.AssertIsFailure(uutMaybe,
      actualFailureValue => Assert.Equal("test", actualFailureValue.ErrorMessage));
  }
}
