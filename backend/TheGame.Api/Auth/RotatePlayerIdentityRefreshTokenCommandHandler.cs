using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.CommandHandlers;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.Utils;

namespace TheGame.Api.Auth;

public sealed record RotatePlayerIdentityRefreshTokenCommand(long PlayerIdentityId,
  string CurrentRefreshToken,
  ushort NewRefreshTokenByteCount,
  uint NewRefreshTokenAgeMinutes) : IRequest<Result<RotatePlayerIdentityRefreshTokenResult>>;

public sealed record RotatePlayerIdentityRefreshTokenResult(string RefreshToken,
  DateTimeOffset RefreshTokenExpiration,
  long PlayerIdentityId,
  long PlayerId,
  string ProviderName,
  string ProviderIdentityId);

public sealed class RotatePlayerIdentityRefreshTokenCommandHandler(IGameDbContext gameDb,
  ITransactionExecutionWrapper transactionWrapper,
  ISystemService systemService,
  ILogger<RotatePlayerIdentityRefreshTokenCommandHandler> logger)
  : IRequestHandler<RotatePlayerIdentityRefreshTokenCommand, Result<RotatePlayerIdentityRefreshTokenResult>>
{
  public async Task<Result<RotatePlayerIdentityRefreshTokenResult>> Handle(RotatePlayerIdentityRefreshTokenCommand request, CancellationToken cancellationToken) =>
    await transactionWrapper.ExecuteInTransaction<RotatePlayerIdentityRefreshTokenResult>(
      async () =>
      {
        logger.LogInformation("Validating command...");

        var playerIdentity = await gameDb.PlayerIdentities
          .Include(ident => ident.Player)
          .Where(ident =>
            ident.RefreshToken == request.CurrentRefreshToken &&
            ident.Id == request.PlayerIdentityId &&
            ident.Player != null)
          .FirstOrDefaultAsync(cancellationToken);

        if (playerIdentity is null)
        {
          logger.LogError("Player identity with supplied id and refresh token is not found. Execution cannot continue.");
          return new Failure(ErrorMessageProvider.PlayerNotFoundError);
        }

        var newTokenResult = playerIdentity.RotateRefreshToken(systemService,
          request.NewRefreshTokenByteCount,
          TimeSpan.FromMinutes(request.NewRefreshTokenAgeMinutes));
        if (!newTokenResult.TryGetSuccessful(out _, out var rotationFailure))
        {
          return rotationFailure;
        }

        await gameDb.SaveChangesAsync(cancellationToken);

        return new RotatePlayerIdentityRefreshTokenResult(playerIdentity.RefreshToken!,
          playerIdentity.RefreshTokenExpiration.GetValueOrDefault(),
          playerIdentity.Id,
          playerIdentity.Player!.Id,
          playerIdentity.ProviderName,
          playerIdentity.ProviderIdentityId);
      },
      nameof(RotatePlayerIdentityRefreshTokenCommand),
      logger,
      cancellationToken);
}
