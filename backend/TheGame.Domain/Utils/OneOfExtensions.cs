using OneOf;
using System.Diagnostics.CodeAnalysis;

namespace TheGame.Domain.Utils
{
  public static class OneOfExtensions
  {
    /// <summary>
    /// Try Get Successful result
    /// </summary>
    /// <typeparam name="TSuccess"></typeparam>
    /// <typeparam name="TFailure"></typeparam>
    /// <param name="oneOfResult"></param>
    /// <param name="success"></param>
    /// <param name="failure"></param>
    /// <returns></returns>
    public static bool TryGetSuccessful<TSuccess, TFailure>(this OneOf<TSuccess, TFailure> oneOfResult,
      [MaybeNullWhen(false)]out TSuccess success,
      [MaybeNullWhen(true)] out TFailure failure)
      where TFailure : Failure
    {
      if (oneOfResult.TryPickT0(out var successfulResult, out var otherResult))
      {
        success = successfulResult;
        failure = default;
        return true;
      }
      else
      {
        success = default;
        failure = otherResult;
        return false;
      }
    }
  }
}
