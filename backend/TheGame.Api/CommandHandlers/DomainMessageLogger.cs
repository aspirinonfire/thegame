using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Api.CommandHandlers;

public sealed class DomainMessageLogger<TDomainMessage>(ILogger<DomainMessageLogger<TDomainMessage>> logger)
  : INotificationHandler<TDomainMessage>
    where TDomainMessage : IDomainEvent
{
  public Task Handle(TDomainMessage notification, CancellationToken cancellationToken)
  {
    logger.LogInformation("Issued domain message {messageType}: {message}",
      typeof(TDomainMessage).Name,
      notification.ToString());

    return Task.CompletedTask;
  }
}
