using System;

namespace TheGame.Domain.Utils
{
  /// <summary>
  /// Failure object
  /// </summary>
  /// <param name="Error"></param>
  public class Failure(string Error)
  {
    public virtual string ErrorMessage => Error;
    public virtual Exception GetException() => new(Error);
  }
}
