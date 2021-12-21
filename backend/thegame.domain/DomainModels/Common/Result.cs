namespace TheGame.Domain.DomainModels.Common
{

  public record Result
  {
    public string ErrorMessage { get; init; }
    public bool IsSuccess { get; init; }

    public static Result<T> Success<T>(T value) where T : BaseModel =>
        new()
        {
          IsSuccess = true,
          Value = value
        };

    public static Result<T> Error<T>(string errorMessage) where T : BaseModel =>
        new()
        {
          IsSuccess = false,
          ErrorMessage = errorMessage
        };
  }

  public record Result<T> : Result where T : BaseModel
  {
    public T Value { get; init; }
  }
}
