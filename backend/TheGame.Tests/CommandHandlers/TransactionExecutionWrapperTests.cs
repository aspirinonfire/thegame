using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using TheGame.Domain.CommandHandlers;
using TheGame.Domain.DomainModels;

namespace TheGame.Tests.CommandHandlers;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
public class TransactionExecutionWrapperTests
{
  [Fact]
  public async Task WillCommitTransactionOnCommandSuccess()
  {
    var transaction = Substitute.For<IDbContextTransaction>();
    var gameDb = Substitute.For<IGameDbContext>();
    gameDb.BeginTransactionAsync().Returns(transaction);

    using var cTokenRegistration = new CancellationTokenRegistration();
    var ctoken = cTokenRegistration.Token;

    var handlerWithSuccess = () => Task.FromResult<Result<string>>("hello world!");

    var actualResult = await TransactionExecutionWrapper.ExecuteOperation(gameDb,
      handlerWithSuccess,
      "test command",
      NullLogger.Instance,
      ctoken);

    actualResult.AssertIsSucceessful();

    await transaction.Received(0).RollbackAsync(ctoken);
    await transaction.Received(1).CommitAsync(ctoken);
  }

  [Fact]
  public async Task WillRollbackTransactionOnCommandError()
  {
    var transaction = Substitute.For<IDbContextTransaction>();
    var gameDb = Substitute.For<IGameDbContext>();
    gameDb.BeginTransactionAsync().Returns(transaction);

    using var cTokenRegistration = new CancellationTokenRegistration();
    var ctoken = cTokenRegistration.Token;

    var handlerWithSuccess = () => Task.FromResult<Result<string>>(new Failure("test error"));

    var actualResult = await TransactionExecutionWrapper.ExecuteOperation(gameDb,
      handlerWithSuccess,
      "test command",
      NullLogger.Instance,
      ctoken);

    actualResult.AssertIsFailure();

    await transaction.Received(1).RollbackAsync(ctoken);
    await transaction.Received(0).CommitAsync(ctoken);
  }
}
