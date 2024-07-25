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
    builder.Logging.AddOpenTelemetry(logging =>
    {
      logging.IncludeFormattedMessage = true;
    });

    var otelConfig = builder.Configuration.GetSection("Otel").Get<OtelConfig>();

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

        if (!string.IsNullOrEmpty(otelConfig?.ExporterEndpoint))
        {
          metrics.AddOtlpExporter(opts =>
            CreateExporterOptions(opts, otelConfig.ExporterEndpoint, otelConfig.ExporterApiKey));
        }
        else
        {
          metrics.AddConsoleExporter();
        }
      })
      // Configure tracing
      .WithTracing(tracing =>
      {
        tracing
          .AddAspNetCoreInstrumentation()
          .AddHttpClientInstrumentation();

        if (!string.IsNullOrEmpty(otelConfig?.ExporterEndpoint))
        {
          tracing.AddOtlpExporter(opts =>
            CreateExporterOptions(opts, otelConfig.ExporterEndpoint, otelConfig.ExporterApiKey));
        }
        else
        {
          tracing.AddConsoleExporter();
        }
      })
      // Configure logging
      .WithLogging(logging =>
      {
        if (!string.IsNullOrEmpty(otelConfig?.ExporterEndpoint))
        {
          logging.AddOtlpExporter(opts =>
            CreateExporterOptions(opts, otelConfig.ExporterEndpoint, otelConfig.ExporterApiKey));
        }
        else
        {
          logging.AddConsoleExporter();
        }
      });
  }

  private static void CreateExporterOptions(OtlpExporterOptions otlpExporterOptions, string otelEndpoint, string? otelApiKey)
  {
    otlpExporterOptions.Protocol = OtlpExportProtocol.Grpc;
    otlpExporterOptions.Endpoint = new Uri(otelEndpoint);
    otlpExporterOptions.Headers = $"x-otlp-api-key={otelApiKey}";
  }
}
