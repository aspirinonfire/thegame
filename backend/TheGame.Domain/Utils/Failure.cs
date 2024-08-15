using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TheGame.Domain.Utils;

/// <summary>
/// Failure object
/// </summary>
/// <param name="Error"></param>
public record Failure(string Error)
{
  public static Failure CreateFromValidationErrors(IEnumerable<ValidationFailure> validationFailures)
  {
    var validationErrors = string.Join("\n", validationFailures.Select(err => $"{err.PropertyName}: {err.ErrorMessage}"));

    return new Failure(validationErrors);
  }

  public virtual string ErrorMessage => Error;
  public virtual Exception GetException() => new(Error);
}
