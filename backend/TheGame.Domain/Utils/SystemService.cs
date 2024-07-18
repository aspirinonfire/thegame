using System;
using System.Security.Cryptography;

namespace TheGame.Domain.Utils;

/// <summary>
/// DateTime wrapper
/// </summary>
public interface ISystemService
{
  IDateTimeService DateTime { get; }
  IDateTimeOffsetService DateTimeOffset { get; }
  string GenerateRandomBase64String(ushort byteCount);
}

internal class SystemService : ISystemService
{
  public IDateTimeService DateTime { get; } = new StaticDateTimeService();

  public IDateTimeOffsetService DateTimeOffset { get; } = new StaticDateTimeOffsetService();

  public string GenerateRandomBase64String(ushort byteCount)
  {
    var randomBytes = RandomNumberGenerator.GetBytes(byteCount);
    return Convert.ToBase64String(randomBytes);
  }

  private class StaticDateTimeService : IDateTimeService
  {
    public DateTime Now => System.DateTime.Now;
  }

  private class StaticDateTimeOffsetService : IDateTimeOffsetService
  {
    public DateTimeOffset Now => System.DateTimeOffset.Now;

    public DateTimeOffset UtcNow => System.DateTimeOffset.UtcNow;
  }
}
