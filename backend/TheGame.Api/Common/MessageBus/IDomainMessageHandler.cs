using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Api.Common.MessageBus;

public interface IDomainMessageHandler<in TDomainMessage>
  where TDomainMessage : IDomainEvent
{
  Task Handle(TDomainMessage notification, CancellationToken cancellationToken);
}
