using System.Diagnostics.CodeAnalysis;

namespace TheGame.Domain.Utils;

public readonly struct Result<TSuccess>
{
  private readonly TSuccess _successValue;
  
  private readonly Failure _failureValue;
  
  private readonly bool _isSuccess;

  // TODO handle validation failures
  private Result(TSuccess? successValue, Failure? failureValue, bool isSuccess)
  {
    // the only way to access success/failure values is through TryGetSuccessful, which has appropriate checks
    _successValue = successValue!;
    _failureValue = failureValue!;
    _isSuccess = isSuccess;
  }

  public static implicit operator Result<TSuccess>(TSuccess success) => new(success, default, true);

  public static implicit operator Result<TSuccess>(Failure failure) => new(default, failure, false);

  /// <summary>
  /// Try Get Successful result
  /// </summary>
  /// <param name="success"></param>
  /// <param name="failure"></param>
  /// <returns></returns>
  public bool TryGetSuccessful([MaybeNullWhen(false)] out TSuccess success, [MaybeNullWhen(true)] out Failure failure)
  {
    if (_isSuccess)
    {
      success = _successValue;
      failure = default;
      return true;
    }
    else
    {
      success = default;
      failure = _failureValue;
      return false;
    }
  }

  public override string ToString() => _isSuccess switch
  {
    true => _successValue?.ToString(),
    _ => _failureValue?.ToString(),
  } ?? string.Empty;
}

public readonly struct Success { }