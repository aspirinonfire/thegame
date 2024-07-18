using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;

namespace TheGame.Domain.CommandHandlers
{
  public sealed record RotatePlayerIdentityRefreshTokenCommand(long PlayerId) : IRequest<OneOf<NewRefreshToken, Failure>>;

  public sealed record NewRefreshToken(string RefreshToken);

  public sealed class RotatePlayerIdentityRefreshTokenCommandHandler(IGameDbContext gameDb, ITransactionExecutionWrapper transactionWrapper, ILogger<RotatePlayerIdentityRefreshTokenCommandHandler> logger)
    : IRequestHandler<RotatePlayerIdentityRefreshTokenCommand, OneOf<NewRefreshToken, Failure>>
  {
    public async Task<OneOf<NewRefreshToken, Failure>> Handle(RotatePlayerIdentityRefreshTokenCommand request, CancellationToken cancellationToken) =>
      await transactionWrapper.ExecuteInTransaction<NewRefreshToken>(
        async () =>
        {
          // TODO implement

          return new NewRefreshToken(string.Empty);
        },
        nameof(RotatePlayerIdentityRefreshTokenCommand),
        logger,
        cancellationToken);
  }
}
