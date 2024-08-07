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
      Assert.Fail($"Expected successful result got failure: {failure?.ErrorMessage}");
      throw new UnreachableException();
    }

    success = successResult;
    successAssertions?.Invoke(successResult);
  }

  public static void AssertIsSucceessful<TSuccess>(this Result<TSuccess> toAssert, Action<TSuccess>? successAssertions = null) =>
    toAssert.AssertIsSucceessful(out _, successAssertions);

  public static void AssertIsFailure<TSuccess>(this Result<TSuccess> toAssert,
    Action<Failure>? failureAssertions = null)
  {
    if (toAssert.TryGetSuccessful(out var successResult, out var failure))
    {
      Assert.Fail($"Expected failure result got success: {successResult}");
    }

    failureAssertions?.Invoke(failure);
  }
}
