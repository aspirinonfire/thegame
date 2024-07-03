using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.PlayerIdentities;

namespace TheGame.Domain.CommandHandlers;

public sealed record GetOrCreateNewPlayerCommand(NewPlayerIdentityRequest NewPlayerIdentityRequest) : IRequest<OneOf<GetOrCreatePlayerResult, Failure>>;

public sealed record GetOrCreatePlayerResult(long PlayerIdentityId, long PlayerId);

public class GetOrCreateNewPlayerCommandHandler(IGameDbContext gameDb, IPlayerIdentityFactory playerIdentityFactory, ITransactionExecutionWrapper transactionWrapper, ILogger<GetOrCreateNewPlayerCommand> logger)
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
              logger.LogInformation("Found an existing player with identity. Returning...");
              return new GetOrCreatePlayerResult(existingPlayer.Id, existingPlayer.Player?.Id ?? -1);
            }

            logger.LogInformation("Attempting to create new player with identity.");

            var newIdentityResult = playerIdentityFactory.CreatePlayerIdentity(request.NewPlayerIdentityRequest);
            if (!newIdentityResult.TryGetSuccessful(out var success, out var failure))
            {
              logger.LogError(failure.GetException(), "New player cannot be created.");
              return failure;
            }
            
            await gameDb.SaveChangesAsync();
            
            logger.LogInformation("New player with identity was created successfully.");
            
            return new GetOrCreatePlayerResult(success.Id, success.Player?.Id ?? -1);
          },
          nameof(GetOrCreateNewPlayerCommand),
          logger,
          cancellationToken);
}
