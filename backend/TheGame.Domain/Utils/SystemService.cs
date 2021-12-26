using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
