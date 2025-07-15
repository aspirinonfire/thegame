using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace TheGame.Domain.Utils;

/// <summary>
/// Failure object
/// </summary>
/// <param name="Error"></param>
public record Failure(string Error)
{
  public virtual Exception GetException() => new(Error);

  public bool TryGetValidationFailure([MaybeNullWhen(false)] out Validation validationFailure)
  {
    if (this is Validation validation)
    {
      validationFailure = validation;
      return true;
    }

    validationFailure = default;
    return false;
  }

  public sealed record Validation : Failure
  {
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; init; }

    public Validation(IEnumerable<FluentValidation.Results.ValidationFailure> validationFailures) : base(CreateErrorMessage(validationFailures))
    {
      ValidationErrors = validationFailures
        .GroupBy(v => v.PropertyName)
        .ToDictionary(grp => grp.Key,
          grp => grp.Select(v => v.ErrorMessage).ToArray());
    }

    public Validation(FluentValidation.Results.ValidationFailure validationFailure) : this([validationFailure])
    { }

    private static string CreateErrorMessage(IEnumerable<FluentValidation.Results.ValidationFailure> validationFailures) =>
      string.Join("\n", validationFailures.Select(err => $"{err.PropertyName}: {err.ErrorMessage}"));
  }
}