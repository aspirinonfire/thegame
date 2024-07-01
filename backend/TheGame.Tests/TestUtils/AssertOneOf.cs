using System.Diagnostics.CodeAnalysis;

namespace TheGame.Tests.TestUtils
{
  public static class AssertOneOf
  {
    public static bool AssertIsSucceessful<TSuccess>(this OneOf<TSuccess, Failure> toAssert,
      [MaybeNullWhen(false)]out TSuccess success,
      Action<TSuccess>? successAssertions = null)
    {
      if (!toAssert.TryGetSuccessful(out var successResult, out var failure))
      {
        success = default;
        Assert.Fail($"Expected successful result got failure: {failure.ErrorMessage}");
        return false;
      }

      success = successResult;
      successAssertions?.Invoke(successResult);
      return true;
    }

    public static bool AssertIsSucceessful<TSuccess>(this OneOf<TSuccess, Failure> toAssert, Action<TSuccess>? successAssertions = null) =>
      toAssert.AssertIsSucceessful(out _, successAssertions);

    public static void AssertIsFailure<TSuccess>(this OneOf<TSuccess, Failure> toAssert,
      Action<Failure>? failureAssertions = null)
    {
      if (toAssert.TryGetSuccessful(out var successResult, out var failure))
      {
        Assert.Fail($"Expected failure result got success: {successResult}");
      }

      failureAssertions?.Invoke(failure);
    }
  }
}
