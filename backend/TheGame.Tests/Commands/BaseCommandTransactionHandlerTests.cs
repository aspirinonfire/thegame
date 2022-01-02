using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.Commands;
using TheGame.Domain.DAL;
using TheGame.Tests.TestUtils;
using Xunit;

namespace TheGame.Tests.Commands
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
  public class BaseCommandTransactionHandlerTests
  {
    [Fact]
    public async Task CanCommitTransactionOnCommandSuccess()
    {
      var trx = new Mock<IDbContextTransaction>();
      var uow = new Mock<IGameUoW>();
      uow.Setup(svc => svc.BeginTransactionAsync()).ReturnsAsync(trx.Object);

      using var cTokenRegistration = new CancellationTokenRegistration();
      var ctoken = cTokenRegistration.Token;

      var cmdInput = new TestCommand();
      var expectedCmdResult = CommandResult.Success(new TestCommandResult());

      var uut = new TestCommandHandler(uow.Object,
        (cmd) => Task.FromResult(expectedCmdResult));

      var actual = await uut.Handle(cmdInput, ctoken);

      Assert.Equal(expectedCmdResult, actual);
      trx.Verify(trx => trx.RollbackAsync(ctoken), Times.Never);
      trx.Verify(trx => trx.CommitAsync(ctoken), Times.Once);
    }

    [Fact]
    public async Task CanRollbackTransactionOnCommandError()
    {
      var trx = new Mock<IDbContextTransaction>();
      var uow = new Mock<IGameUoW>();
      uow.Setup(svc => svc.BeginTransactionAsync()).ReturnsAsync(trx.Object);

      using var cTokenRegistration = new CancellationTokenRegistration();
      var ctoken = cTokenRegistration.Token;

      var cmdInput = new TestCommand();
      var expectedCmdResult = CommandResult.Error<TestCommandResult>("test error");

      var uut = new TestCommandHandler(uow.Object,
        (cmd) => Task.FromResult(expectedCmdResult));

      var actual = await uut.Handle(cmdInput, ctoken);

      Assert.Equal(expectedCmdResult, actual);
      trx.Verify(trx => trx.RollbackAsync(ctoken), Times.Once);
      trx.Verify(trx => trx.CommitAsync(ctoken), Times.Never);
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
