namespace TheGame.Domain.DomainModels.Common
{

  public record DomainResult
  {
    public string? ErrorMessage { get; init; }
    public bool IsSuccess { get; init; }

    public static DomainResult<T> Success<T>(T? value) where T : BaseModel =>
        new()
        {
          IsSuccess = true,
          Value = value
        };

    public static DomainResult<T> Error<T>(string errorMessage) where T : BaseModel =>
        new()
        {
          IsSuccess = false,
          ErrorMessage = errorMessage
        };
  }

  public record DomainResult<T> : DomainResult where T : BaseModel
  {
    public T? Value { get; init; }

    public bool HasNoValue => Value is null;
  }
}
