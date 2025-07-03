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
    private readonly static TimeSpan _defaultWaitTime = TimeSpan.FromSeconds(1);

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

      var invoked = new TaskCompletionSource();
      var testHandler = Substitute.For<IDomainMessageHandler<TestMessage>>();
      testHandler
        .Handle(testMessage, Arg.Any<CancellationToken>())
        .Returns(_ => Task.Run(invoked.SetResult));

      var services = new ServiceCollection()
        .AddLogging(builder => builder.AddDebug())
        .AddScoped(_ => testHandler);

      using var serviceProvider = services.BuildServiceProvider();

      var messageQueue = new ChannelsMessageQueue(GetQueueOptions());

      var uutProcessor = new DomainMessagesProcessor(
        messageQueue,
        serviceProvider.GetRequiredService<IServiceScopeFactory>(),
        serviceProvider.GetRequiredService<ILogger<DomainMessagesProcessor>>());

      // write a single message and complete the queue
      messageQueue.Writer.TryWrite(testMessage);
      messageQueue.Writer.Complete();

      // wait for the queue processor to process a message
      await uutProcessor.ListenAndProcessQueueEvents(CancellationToken.None);

      await invoked.Task.WaitAsync(_defaultWaitTime);

      await testHandler
        .Received(1)
        .Handle(testMessage, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WillProcessDomainMessageWithTwoHandlers()
    {
      var testMessage = new TestMessage();

      var invoked1 = new TaskCompletionSource();
      var testHandler1 = Substitute.For<IDomainMessageHandler<TestMessage>>();
      testHandler1
        .Handle(testMessage, Arg.Any<CancellationToken>())
        .Returns(_ => Task.Run(invoked1.SetResult));

      var invoked2 = new TaskCompletionSource();
      var testHandler2 = Substitute.For<IDomainMessageHandler<TestMessage>>();
      testHandler2
        .Handle(testMessage, Arg.Any<CancellationToken>())
        .Returns(_ => Task.Run(invoked2.SetResult));

      var services = new ServiceCollection()
        .AddLogging(builder => builder.AddDebug())
        .AddScoped(_ => testHandler1)
        .AddScoped(_ => testHandler2);

      using var serviceProvider = services.BuildServiceProvider();

      var messageQueue = new ChannelsMessageQueue(GetQueueOptions());

      var uutProcessor = new DomainMessagesProcessor(
        messageQueue,
        serviceProvider.GetRequiredService<IServiceScopeFactory>(),
        serviceProvider.GetRequiredService<ILogger<DomainMessagesProcessor>>());

      // write a single message and complete the queue
      messageQueue.Writer.TryWrite(testMessage);
      messageQueue.Writer.Complete();

      // wait for the queue processor to process a message
      await uutProcessor.ListenAndProcessQueueEvents(CancellationToken.None);

      await Task.WhenAll(
        invoked1.Task.WaitAsync(_defaultWaitTime),
        invoked2.Task.WaitAsync(_defaultWaitTime));

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

      var messageQueue = new ChannelsMessageQueue(GetQueueOptions());

      var uutProcessor = new DomainMessagesProcessor(
        messageQueue,
        serviceProvider.GetRequiredService<IServiceScopeFactory>(),
        serviceProvider.GetRequiredService<ILogger<DomainMessagesProcessor>>());

      // write a single message and complete the queue
      messageQueue.Writer.TryWrite(testMessage);
      messageQueue.Writer.Complete();

      // wait for the queue processor to process a message
      var actualException = await Record.ExceptionAsync(() => uutProcessor.ListenAndProcessQueueEvents(CancellationToken.None));

      Assert.Null(actualException);
    }

    [Fact]
    public async Task WillHandleExceptionWhenOneHandlerErrorsOut()
    {
      var testMessage = new TestMessage();

      var invoked1 = new TaskCompletionSource();
      var testHandler1 = Substitute.For<IDomainMessageHandler<TestMessage>>();
      testHandler1
        .Handle(testMessage, Arg.Any<CancellationToken>())
        .Returns(_ =>
        {
          invoked1.SetResult();
          return Task.FromException(new InvalidOperationException("Test exception from handler 1"));
        });

      var invoked2 = new TaskCompletionSource();
      var testHandler2 = Substitute.For<IDomainMessageHandler<TestMessage>>();
      testHandler2
        .Handle(testMessage, Arg.Any<CancellationToken>())
        .Returns(_ =>
        {
          invoked2.SetResult();
          return Task.CompletedTask;
        });

      var services = new ServiceCollection()
        .AddLogging(builder => builder.AddDebug())
        .AddScoped(_ => testHandler1)
        .AddScoped(_ => testHandler2);

      using var serviceProvider = services.BuildServiceProvider();

      var messageQueue = new ChannelsMessageQueue(GetQueueOptions());

      var uutProcessor = new DomainMessagesProcessor(
        messageQueue,
        serviceProvider.GetRequiredService<IServiceScopeFactory>(),
        serviceProvider.GetRequiredService<ILogger<DomainMessagesProcessor>>());

      // write a single message and complete the queue
      messageQueue.Writer.TryWrite(testMessage);
      messageQueue.Writer.Complete();

      // wait for the queue processor to process a message
      await uutProcessor.ListenAndProcessQueueEvents(CancellationToken.None);

      await Task.WhenAll(
        invoked1.Task.WaitAsync(_defaultWaitTime),
        invoked2.Task.WaitAsync(_defaultWaitTime));

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
