namespace TheGame.Domain.Utils
{
  /// <summary>
  /// DateTime wrapper
  /// </summary>
  public interface ISystemService
  {
    IDateTimeService DateTime { get; }
    IDateTimeOffsetService DateTimeOffset { get; }
  }
}
