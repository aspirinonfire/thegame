using Microsoft.EntityFrameworkCore.Storage;
using TheGame.Domain.CommandHandlers;
using TheGame.Domain.DomainModels;
using TheGame.Tests.TestUtils;

namespace TheGame.Tests.CommandHandlers
{
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

      var handlerWithSuccess = () => Task.FromResult<OneOf<string, Failure>>("hello world!");

      var uutWrapper = new TransactionExecutionWrapper(gameDb);

      var actualResult = await uutWrapper.ExecuteInTransaction(handlerWithSuccess, ctoken);

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

      var handlerWithSuccess = () => Task.FromResult<OneOf<string, Failure>>(new Failure("test error"));

      var uutWrapper = new TransactionExecutionWrapper(gameDb);

      var actualResult = await uutWrapper.ExecuteInTransaction(handlerWithSuccess, ctoken);

      actualResult.AssertIsFailure();

      await transaction.Received(1).RollbackAsync(ctoken);
      await transaction.Received(0).CommitAsync(ctoken);
    }
  }
}
