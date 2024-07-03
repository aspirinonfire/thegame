using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.PlayerIdentities;

namespace TheGame.Domain.CommandHandlers;

public sealed record GetOrCreateNewPlayerCommand(NewPlayerIdentityRequest NewPlayerIdentityRequest) : IRequest<OneOf<GetOrCreatePlayerResult, Failure>>;

public sealed record GetOrCreatePlayerResult(long PlayerIdentityId, long PlayerId);

public class GetOrCreateNewPlayerCommandHandler(IGameDbContext gameDb, IPlayerIdentityFactory playerIdentityFactory, ITransactionExecutionWrapper transactionWrapper)
  : IRequestHandler<GetOrCreateNewPlayerCommand, OneOf<GetOrCreatePlayerResult, Failure>>
{
    public async Task<OneOf<GetOrCreatePlayerResult, Failure>> Handle(GetOrCreateNewPlayerCommand request, CancellationToken cancellationToken) =>
      await transactionWrapper.ExecuteInTransaction<GetOrCreatePlayerResult>(
          async () =>
          {
            var existingPlayer = gameDb.PlayerIdentities
              .AsNoTracking()
              .Include(ident => ident.Player)
              .Where(ident =>
                ident.ProviderName == request.NewPlayerIdentityRequest.ProviderName &&
                ident.ProviderIdentityId == request.NewPlayerIdentityRequest.ProviderIdentityId)
              .FirstOrDefault();

            if (existingPlayer != null)
            {
              return new GetOrCreatePlayerResult(existingPlayer.Id, existingPlayer.Player?.Id ?? -1);
            }

            var newIdentityResult = playerIdentityFactory.CreatePlayerIdentity(request.NewPlayerIdentityRequest);
            if (newIdentityResult.TryGetSuccessful(out var success, out var failure))
            {
              await gameDb.SaveChangesAsync();
              return new GetOrCreatePlayerResult(success.Id, success.Player?.Id ?? -1);
            }
            else
            {
              return failure;
            }
          },
          cancellationToken);
}
