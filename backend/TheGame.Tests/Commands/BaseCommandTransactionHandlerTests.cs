using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using TheGame.Domain.Commands;
using TheGame.Domain.DAL;
using TheGame.Tests.TestUtils;

namespace TheGame.Tests.Commands
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
  public class BaseCommandTransactionHandlerTests
  {
    [Fact]
    public async Task WillCommitTransactionOnCommandSuccess()
    {
      var transaction = Substitute.For<IDbContextTransaction>();
      var gameUow = Substitute.For<IGameUoW>();
      gameUow.BeginTransactionAsync().Returns(transaction);

      using var cTokenRegistration = new CancellationTokenRegistration();
      var ctoken = cTokenRegistration.Token;

      var cmdInput = new TestCommand();
      var expectedCmdResult = CommandResult.Success(new TestCommandResult());

      var uut = new TestCommandHandler(gameUow,
        (cmd) => Task.FromResult(expectedCmdResult));

      var actual = await uut.Handle(cmdInput, ctoken);

      Assert.Equal(expectedCmdResult, actual);

      await transaction.Received(0).RollbackAsync(ctoken);
      await transaction.Received(1).CommitAsync(ctoken);
    }

    [Fact]
    public async Task WillRollbackTransactionOnCommandError()
    {
      var transaction = Substitute.For<IDbContextTransaction>();
      var gameUow = Substitute.For<IGameUoW>();
      gameUow.BeginTransactionAsync().Returns(transaction);

      using var cTokenRegistration = new CancellationTokenRegistration();
      var ctoken = cTokenRegistration.Token;

      var cmdInput = new TestCommand();
      var expectedCmdResult = CommandResult.Error<TestCommandResult>("test error");

      var uut = new TestCommandHandler(gameUow,
        (cmd) => Task.FromResult(expectedCmdResult));

      var actual = await uut.Handle(cmdInput, ctoken);

      Assert.Equal(expectedCmdResult, actual);

      await transaction.Received(1).RollbackAsync(ctoken);
      await transaction.Received(0).CommitAsync(ctoken);
    }
  }

  public class TestCommandResult: ICommandResult
  { }

  public class TestCommand : IRequest<CommandResult<TestCommandResult>>
  { }

  public class TestCommandHandler : BaseCommandTransactionHandler<TestCommand, TestCommandResult>
  {
    private readonly Func<TestCommand, Task<CommandResult<TestCommandResult>>> _handler;

    public TestCommandHandler(IGameUoW gameUoW, Func<TestCommand, Task<CommandResult<TestCommandResult>>> cmdRunner) :
      base(gameUoW)
    {
      _handler = cmdRunner;
    }

    protected override Task<CommandResult<TestCommandResult>> ExecuteCommand(TestCommand command, CancellationToken cancellationToken) =>
      _handler(command);
  }
}
