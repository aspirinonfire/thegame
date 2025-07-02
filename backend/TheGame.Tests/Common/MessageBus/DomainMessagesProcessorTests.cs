using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheGame.Api;
using TheGame.Api.Common.MessageBus;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Tests.Common.MessageBus
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
  public class DomainMessagesProcessorTests
  {
    private static IOptions<GameSettings> GetQueueOptions()
    {
      var options = Substitute.For<IOptions<GameSettings>>();
      options.Value.Returns(new GameSettings
      {
        Auth = null!,
        Otel = null,
        DomainEventsMessageBus = new MessageBusSettings { MaxQueueSize = 10 }
      });

      return options;
    }

    [Fact]
    public async Task WillProcessDomainMessageWithSingleHandler()
    {
      var testMessage = new TestMessage();

      var testHandler = Substitute.For<IDomainMessageHandler<TestMessage>>();

      var services = new ServiceCollection()
        .AddLogging(builder => builder.AddDebug())
        .AddScoped(_ => testHandler);

      using var serviceProvider = services.BuildServiceProvider();

      var messageQueue = new InMemoryMessageQueue(GetQueueOptions());

      var uutProcessor = new DomainMessagesProcessor(
        messageQueue,
        serviceProvider.GetRequiredService<IServiceScopeFactory>(),
        serviceProvider.GetRequiredService<ILogger<DomainMessagesProcessor>>());

      // write a single message and complete the queue
      messageQueue.Writer.TryWrite(testMessage);
      messageQueue.Writer.Complete();

      // wait for the queue processor to process a message
      await uutProcessor.ListenAndProcessQueueEvents(CancellationToken.None);

      // Allow some time for processing (we are dealing with fire-and-forget tasks)
      await Task.Delay(100);

      await testHandler
        .Received(1)
        .Handle(testMessage, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WillProcessDomainMessageWithTwoHandlers()
    {
      var testMessage = new TestMessage();

      var testHandler1 = Substitute.For<IDomainMessageHandler<TestMessage>>();
      var testHandler2 = Substitute.For<IDomainMessageHandler<TestMessage>>();

      var services = new ServiceCollection()
        .AddLogging(builder => builder.AddDebug())
        .AddScoped(_ => testHandler1)
        .AddScoped(_ => testHandler2);

      using var serviceProvider = services.BuildServiceProvider();

      var messageQueue = new InMemoryMessageQueue(GetQueueOptions());

      var uutProcessor = new DomainMessagesProcessor(
        messageQueue,
        serviceProvider.GetRequiredService<IServiceScopeFactory>(),
        serviceProvider.GetRequiredService<ILogger<DomainMessagesProcessor>>());

      // write a single message and complete the queue
      messageQueue.Writer.TryWrite(testMessage);
      messageQueue.Writer.Complete();

      // wait for the queue processor to process a message
      await uutProcessor.ListenAndProcessQueueEvents(CancellationToken.None);

      // Allow some time for processing (we are dealing with fire-and-forget tasks)
      await Task.Delay(100);

      await testHandler1
        .Received(1)
        .Handle(testMessage, Arg.Any<CancellationToken>());

      await testHandler2
        .Received(1)
        .Handle(testMessage, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WillProcessDomainMessageWithoutHandler()
    {
      var testMessage = new TestMessage();

      var services = new ServiceCollection()
        .AddLogging(builder => builder.AddDebug());

      using var serviceProvider = services.BuildServiceProvider();

      var messageQueue = new InMemoryMessageQueue(GetQueueOptions());

      var uutProcessor = new DomainMessagesProcessor(
        messageQueue,
        serviceProvider.GetRequiredService<IServiceScopeFactory>(),
        serviceProvider.GetRequiredService<ILogger<DomainMessagesProcessor>>());

      // write a single message and complete the queue
      messageQueue.Writer.TryWrite(testMessage);
      messageQueue.Writer.Complete();

      // wait for the queue processor to process a message
      var actualException = await Record.ExceptionAsync(() => uutProcessor.ListenAndProcessQueueEvents(CancellationToken.None));

      // Allow some time for processing (we are dealing with fire-and-forget tasks)
      await Task.Delay(100);

      Assert.Null(actualException);
    }

    [Fact]
    public async Task WillHandleExceptionWhenOneHandlerErrorsOut()
    {
      var testMessage = new TestMessage();

      var testHandler1 = Substitute.For<IDomainMessageHandler<TestMessage>>();
      testHandler1
        .Handle(testMessage, Arg.Any<CancellationToken>())
        .Returns(Task.FromException(new InvalidOperationException("Test exception from handler 1")));
      
      var testHandler2 = Substitute.For<IDomainMessageHandler<TestMessage>>();

      var services = new ServiceCollection()
        .AddLogging(builder => builder.AddDebug())
        .AddScoped(_ => testHandler1)
        .AddScoped(_ => testHandler2);

      using var serviceProvider = services.BuildServiceProvider();

      var messageQueue = new InMemoryMessageQueue(GetQueueOptions());

      var uutProcessor = new DomainMessagesProcessor(
        messageQueue,
        serviceProvider.GetRequiredService<IServiceScopeFactory>(),
        serviceProvider.GetRequiredService<ILogger<DomainMessagesProcessor>>());

      // write a single message and complete the queue
      messageQueue.Writer.TryWrite(testMessage);
      messageQueue.Writer.Complete();

      // wait for the queue processor to process a message
      await uutProcessor.ListenAndProcessQueueEvents(CancellationToken.None);

      // Allow some time for processing (we are dealing with fire-and-forget tasks)
      await Task.Delay(100);

      await testHandler1
        .Received(1)
        .Handle(testMessage, Arg.Any<CancellationToken>());

      await testHandler2
        .Received(1)
        .Handle(testMessage, Arg.Any<CancellationToken>());
    }

    public sealed record TestMessage() : IDomainEvent;
  }
}
