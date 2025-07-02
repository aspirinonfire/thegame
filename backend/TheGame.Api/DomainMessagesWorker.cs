using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Api.Common.MessageBus;

namespace TheGame.Api;

public class DomainMessagesWorker(DomainMessagesProcessor domainMessagesProcessor) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken cancellationToken)
  {
    await domainMessagesProcessor.ListenAndProcessQueueEvents(cancellationToken);
  }
}
