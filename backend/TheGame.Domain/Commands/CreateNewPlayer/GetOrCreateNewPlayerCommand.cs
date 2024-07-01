using MediatR;
using TheGame.Domain.DomainModels.PlayerIdentities;

namespace TheGame.Domain.Commands.CreateNewPlayer
{
  public sealed record GetOrCreateNewPlayerCommand(NewPlayerIdentityRequest NewPlayerIdentityRequest) : IRequest<OneOf<GetOrCreatePlayerResult, Failure>>;
}
