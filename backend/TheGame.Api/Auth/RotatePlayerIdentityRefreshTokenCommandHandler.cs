using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Api.Common;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.Utils;

namespace TheGame.Api.Auth;

public sealed record RotatePlayerIdentityRefreshTokenCommand(long PlayerIdentityId,
  string CurrentRefreshToken,
  ushort NewRefreshTokenByteCount,
  uint NewRefreshTokenAgeMinutes)
{
  public sealed record Result(string RefreshToken,
    DateTimeOffset RefreshTokenExpiration,
    long PlayerIdentityId,
    long PlayerId,
    string ProviderName,
    string ProviderIdentityId);
}

public sealed class RotatePlayerIdentityRefreshTokenCommandHandler(IGameDbContext gameDb,
  ITransactionExecutionWrapper transactionWrapper,
  TimeProvider timeProvider,
  ILogger<RotatePlayerIdentityRefreshTokenCommandHandler> logger)
    : ICommandHandler<RotatePlayerIdentityRefreshTokenCommand, RotatePlayerIdentityRefreshTokenCommand.Result>
{
  public async Task<Result<RotatePlayerIdentityRefreshTokenCommand.Result>> Execute(RotatePlayerIdentityRefreshTokenCommand command, CancellationToken cancellationToken) =>
    await transactionWrapper.ExecuteInTransaction<RotatePlayerIdentityRefreshTokenCommand.Result>(
      async () =>
      {
        logger.LogInformation("Validating command...");

        var playerIdentity = await gameDb.PlayerIdentities
          .Include(ident => ident.Player)
          .Where(ident =>
            ident.RefreshToken == command.CurrentRefreshToken &&
            ident.Id == command.PlayerIdentityId &&
            ident.Player != null)
          .FirstOrDefaultAsync(cancellationToken);

        if (playerIdentity is null)
        {
          logger.LogError("Player identity with supplied id and refresh token is not found. Execution cannot continue.");
          return new Failure(ErrorMessageProvider.PlayerNotFoundError);
        }

        var newTokenResult = playerIdentity.RotateRefreshToken(timeProvider,
          command.NewRefreshTokenByteCount,
          TimeSpan.FromMinutes(command.NewRefreshTokenAgeMinutes));
        if (!newTokenResult.TryGetSuccessful(out _, out var rotationFailure))
        {
          return rotationFailure;
        }

        await gameDb.SaveChangesAsync(cancellationToken);

        return new RotatePlayerIdentityRefreshTokenCommand.Result(playerIdentity.RefreshToken!,
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
