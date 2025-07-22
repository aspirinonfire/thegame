using System.Diagnostics;

namespace TheGame.Tests.TestUtils;

public static class AssertResult
{
  public static void AssertIsSucceessful<TSuccess>(this Result<TSuccess> toAssert,
    out TSuccess success,
    Action<TSuccess>? successAssertions = null)
  {
    if (!toAssert.TryGetSuccessful(out var successResult, out var failure))
    {
      Assert.Fail($"Expected successful result got failure: {failure?.Error}");
      throw new UnreachableException();
    }

    success = successResult;
    successAssertions?.Invoke(successResult);
  }

  public static TSuccess AssertIsSucceessful<TSuccess>(this Result<TSuccess> toAssert, Action<TSuccess>? successAssertions = null)
  {
    toAssert.AssertIsSucceessful(out var success, successAssertions);
    return success;
  } 

  public static void AssertIsFailure<TSuccess>(this Result<TSuccess> toAssert, Action<Failure>? failureAssertions)
  {
    if (toAssert.TryGetSuccessful(out var successResult, out var failure))
    {
      Assert.Fail($"Expected failure result got success: {successResult}");
    }

    failureAssertions?.Invoke(failure);
  }

  public static Failure AssertIsFailure<TSuccess>(this Result<TSuccess> toAssert)
  {
    if (toAssert.TryGetSuccessful(out var successResult, out var failure))
    {
      Assert.Fail($"Expected failure result got success: {successResult}");
    }

    return failure;
  }
}
