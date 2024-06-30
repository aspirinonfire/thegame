using System;

namespace TheGame.Domain.Utils;

public interface IDateTimeService
{
  DateTime Now { get; }
}
