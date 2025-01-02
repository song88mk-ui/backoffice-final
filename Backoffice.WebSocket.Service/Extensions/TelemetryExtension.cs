using Backoffice.WebSocket.Service.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System;

namespace Backoffice.WebSocket.Service.Extensions
{
    public static class TelemetryExtension
    {
        public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            Action<ResourceBuilder> resource = r => r.AddService(
                    serviceName: "Backoffice.WebSocket.Service",
                    serviceVersion: "latest",
                    serviceInstanceId: Environment.MachineName);

            services.AddSingleton<Instrumentation>();

            if (Convert.ToBoolean(configuration["Telemetry:Enabled"]))
            {
                services
                    .AddOpenTelemetry()
                    .ConfigureResource(resource)
                    .WithTracing(tracing =>
                    {
                        tracing
                        .AddSource(Instrumentation.ActivitySourceName)
                        .SetSampler(new AlwaysOnSampler())
                        .AddOtlpExporter();
                    })
                    .WithMetrics(metrics =>
                    {
                        metrics
                        .AddMeter(Instrumentation.MeterName)
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation()
                        .AddOtlpExporter();
                    });
            }

            return services;
        }

    }
}
