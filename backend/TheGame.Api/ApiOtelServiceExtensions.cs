using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;

namespace TheGame.Api;

public static class ApiOtelServiceExtensions
{
  public static void AddGameApiOpenTelemtry(this WebApplicationBuilder builder)
  {
    var otelConfig = builder.Configuration.GetSection("Otel").Get<OtelConfig>();
    if (string.IsNullOrEmpty(otelConfig?.ExporterEndpoint))
    {
      return;
    }

    builder.Logging.AddOpenTelemetry(logging =>
    {
      logging.IncludeFormattedMessage = true;
    });

    var otel = builder.Services
      .AddOpenTelemetry()
      // Configure OpenTelemetry Resources with the application name
      .ConfigureResource(resource =>
      {
        resource.AddService(serviceName: builder.Environment.ApplicationName);
      })
      // Configure metrics
      .WithMetrics(metrics =>
      {
        metrics
          // Metrics provider from OpenTelemetry
          .AddAspNetCoreInstrumentation()
          .AddHttpClientInstrumentation();

        metrics.AddOtlpExporter(opts =>
          CreateExporterOptions(opts, otelConfig.ExporterEndpoint, otelConfig.ExporterApiKey));
      })
      // Configure tracing
      .WithTracing(tracing =>
      {
        tracing
          .AddAspNetCoreInstrumentation()
          .AddHttpClientInstrumentation()
          .AddEntityFrameworkCoreInstrumentation();

        tracing.AddOtlpExporter(opts =>
          CreateExporterOptions(opts, otelConfig.ExporterEndpoint, otelConfig.ExporterApiKey));
      })
      // Configure logging
      .WithLogging(logging =>
      {
        logging.AddOtlpExporter(opts =>
          CreateExporterOptions(opts, otelConfig.ExporterEndpoint, otelConfig.ExporterApiKey));
      });
  }

  private static void CreateExporterOptions(OtlpExporterOptions otlpExporterOptions, string otelEndpoint, string? otelApiKey)
  {
    otlpExporterOptions.Protocol = OtlpExportProtocol.Grpc;
    otlpExporterOptions.Endpoint = new Uri(otelEndpoint);
    otlpExporterOptions.Headers = $"x-otlp-api-key={otelApiKey}";
  }
}
