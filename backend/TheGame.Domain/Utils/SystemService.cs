using System;

namespace TheGame.Domain.Utils
{
  internal class SystemService : ISystemService
  {
    public IDateTimeService DateTime { get; } = new StaticDateTimeService();

    public IDateTimeOffsetService DateTimeOffset { get; } = new StaticDateTimeOffsetService();

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
}
