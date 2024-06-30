using System;

namespace TheGame.Domain.Utils;

public interface IDateTimeOffsetService
{
  DateTimeOffset Now { get; }
  DateTimeOffset UtcNow { get; }
}
