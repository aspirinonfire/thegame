using MediatR;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DAL;

namespace TheGame.Domain.Commands.CreateTeamAndPlayer
{
  public class CreateTeamAndPlayerCommandHandler : BaseCommandTransactionHandler<CreateTeamAndPlayerCommand, CreateTeamAndPlayerResult>
  {
    public CreateTeamAndPlayerCommandHandler(IGameUoW gameUoW) : base(gameUoW)
    { }

    protected override Task<CommandResult<CreateTeamAndPlayerResult>> ExecuteCommand(CreateTeamAndPlayerCommand command, CancellationToken cancellationToken)
    {
      throw new System.NotImplementedException();
    }
  }
}
