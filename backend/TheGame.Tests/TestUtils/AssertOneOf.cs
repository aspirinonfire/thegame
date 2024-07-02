using System.Diagnostics;

namespace TheGame.Tests.TestUtils
{
  public static class AssertOneOf
  {
    public static void AssertIsSucceessful<TSuccess>(this OneOf<TSuccess, Failure> toAssert,
      out TSuccess success,
      Action<TSuccess>? successAssertions = null)
    {
      if (!toAssert.TryGetSuccessful(out var successResult, out var failure))
      {
        Assert.Fail($"Expected successful result got failure: {failure.ErrorMessage}");
        throw new UnreachableException();
      }

      success = successResult;
      successAssertions?.Invoke(successResult);
    }

    public static void AssertIsSucceessful<TSuccess>(this OneOf<TSuccess, Failure> toAssert, Action<TSuccess>? successAssertions = null) =>
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
