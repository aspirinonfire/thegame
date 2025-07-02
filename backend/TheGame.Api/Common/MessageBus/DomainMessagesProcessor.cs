using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TheGame.Api.Common.MessageBus;

public sealed class DomainMessagesProcessor(
  InMemoryMessageQueue queue,
  IServiceScopeFactory serviceScopeFactory,
  ILogger<DomainMessagesProcessor> logger)
{
  public async Task ListenAndProcessQueueEvents(CancellationToken cancellationToken)
  {
    logger.LogInformation("Starting to listen for events in the message queue...");

    await foreach (var eventToProcess in queue.Reader.ReadAllAsync(cancellationToken))
    {
      if (eventToProcess is null)
      {
        logger.LogWarning("Received a null event. Skipping processing.");
        continue;
      }

      var eventType = eventToProcess.GetType();
      logger.LogInformation("Processing {eventType}...", eventType.Name);

      // Run each event in its own task (fire-and-forget) to avoid blocking the queue reader
      _ = Task.Run(async () =>
      {
        try
        {
          // Resolve all handlers for the event type
          var handlerType = typeof(IDomainMessageHandler<>).MakeGenericType(eventType);

          await using var scope = serviceScopeFactory.CreateAsyncScope();

          var handlers = scope.ServiceProvider.GetServices(handlerType)
            .Where(handler => handler is not null)
            .Cast<object>()
            .ToArray();

          if (handlers.Length == 0)
          {
            logger.LogWarning("No handlers found for event type {eventType}.", eventType.Name);
            return;
          }

          logger.LogInformation("Found {handlerCount} handler(s) for event type {eventType}.",
            handlers.Length,
            eventType.Name);
          
          foreach (var handler in handlers)
          {
            try
            {
              // using dynamic to avoid reflection overhead
              await ((dynamic)handler!).Handle((dynamic)eventToProcess, cancellationToken);
            }
            catch (Exception ex)
            {
              logger.LogError(ex, "Error invoking handler for event type {eventType}.", eventType.Name);
              continue;
            }
          }

          logger.LogInformation("Successfully processed event {eventType}.", eventType.Name);
        }
        catch (Exception ex)
        {
          logger.LogError(ex, "Failed to process event.");
        }
      },
      cancellationToken)
        .ContinueWith(t => logger.LogError(t.Exception, "Error processing event {eventType}.", eventType.Name),
          TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
    }

    logger.LogInformation("Message queue got closed or cancellation was issued. Message processing is now complete.");
  }
}