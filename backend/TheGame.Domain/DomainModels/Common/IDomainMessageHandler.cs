using System.Threading;
using System.Threading.Tasks;

namespace TheGame.Domain.DomainModels.Common;

public interface IDomainMessageHandler<in TDomainMessage>
  where TDomainMessage : IDomainEvent
{
  Task Handle(TDomainMessage notification, CancellationToken cancellationToken);
}
