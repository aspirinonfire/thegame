using Moq;
using System;
using TheGame.Domain.Utils;

namespace TheGame.Tests.TestUtils
{
  public static class CommonMockedServices
  {
    public static readonly DateTimeOffset DefaultDate = new DateTimeOffset(2021, 12, 31, 0, 0, 0, 0, TimeSpan.Zero);

    public static ISystemService GetSystemService(DateTimeOffset? dateTimeOffset = null)
    {
      var currentTimestamp = dateTimeOffset.GetValueOrDefault(DefaultDate);

      var dtOffsetSvc = new Mock<IDateTimeOffsetService>();
      dtOffsetSvc.Setup(svc => svc.UtcNow).Returns(currentTimestamp);
      dtOffsetSvc.Setup(svc => svc.Now).Returns(currentTimestamp);
      var dtSvc = new Mock<IDateTimeService>();
      dtSvc.Setup(svc => svc.Now).Returns(currentTimestamp.DateTime);

      var sysSvc = new Mock<ISystemService>();
      sysSvc.Setup(svc => svc.DateTimeOffset).Returns(dtOffsetSvc.Object);
      sysSvc.Setup(svc => svc.DateTime).Returns(dtSvc.Object);
      return sysSvc.Object;
    }
  }
}
