using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Api.Common;

public sealed class DomainMessageLogger<TDomainMessage>(ILogger<DomainMessageLogger<TDomainMessage>> logger) 
  : IDomainMessageHandler<TDomainMessage>
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