using MediatR;

namespace TheGame.Domain.Commands.CreateTeamAndPlayer
{
  public class CreateTeamAndPlayerCommand : IRequest<CommandResult<CreateTeamAndPlayerResult>>
  {
    public string? TeamName { get; set; }
    public long? UserId { get; set; }
  }
}
