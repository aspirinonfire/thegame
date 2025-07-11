using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TheGame.Api.Common;

public static class GameApiMiddleware
{
  public const string CorrelationIdKey = "GameRequestCorrelationId";

  /// <summary>
  /// Request Correlation middleware
  /// </summary>
  /// <remarks>
  /// This middleware correlates all logs associated with the current request, enabling easier query in App Insights: customDimensions['CorrelationId']
  /// </remarks>
  public static Func<HttpContext, RequestDelegate, Task> CreateRequestCorrelationMiddleware()
  {
    return async (ctx, next) =>
    {
      var logger = ctx.RequestServices
          .GetRequiredService<ILoggerFactory>()
          .CreateLogger(nameof(GameApiMiddleware));

      var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

      ctx.Items[CorrelationIdKey] = correlationId;

      using var scope = logger.BeginScope(new Dictionary<string, object?>
      {
        [CorrelationIdKey] = correlationId
      });

      await next(ctx);
    };
  }

  /// <summary>
  /// Retrieve CorrelationId from HttpContext
  /// </summary>
  /// <param name="ctx"></param>
  /// <returns></returns>
  public static string? RetrieveCorrelationId(this HttpContext ctx) =>
    ctx.Items.TryGetValue(CorrelationIdKey, out var value) && value is string correlationId ?
      correlationId :
      null;
}
