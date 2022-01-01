namespace TheGame.Domain.Commands
{
  public record CommandResult
  {
    public string? ErrorMessage { get; init; }
    public bool IsSuccess { get; init; }

    public static CommandResult<T> Success<T>(T? value) where T : ICommandResult =>
        new()
        {
          IsSuccess = true,
          Value = value
        };

    public static CommandResult<T> Error<T>(string errorMessage) where T : ICommandResult =>
        new()
        {
          IsSuccess = false,
          ErrorMessage = errorMessage
        };
  }

  public record CommandResult<T> : CommandResult where T : ICommandResult
  {
    public T? Value { get; init; }

    public bool HasNoValue => Value is null;
  }
}
