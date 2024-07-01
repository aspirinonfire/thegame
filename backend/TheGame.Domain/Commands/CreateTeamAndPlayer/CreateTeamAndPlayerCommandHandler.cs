using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;

namespace TheGame.Domain.Commands.CreateTeamAndPlayer
{
  public class CreateTeamAndPlayerCommandHandler(IGameDbContext gameDb)
    : BaseCommandTransactionHandler<CreateTeamAndPlayerCommand, CreateTeamAndPlayerResult>(gameDb)
  {
    protected override Task<CommandResult<CreateTeamAndPlayerResult>> ExecuteCommand(CreateTeamAndPlayerCommand command, CancellationToken cancellationToken)
    {
      throw new System.NotImplementedException();
    }
  }
}
