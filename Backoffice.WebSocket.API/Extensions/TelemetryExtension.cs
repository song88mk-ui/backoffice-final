using Backoffice.WebSocket.API.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System;

namespace Backoffice.WebSocket.API.Extensions
{
    public static class TelemetryExtension
    {
        public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            Action<ResourceBuilder> resource = r => r.AddService(
                    serviceName: "Backoffice.WebSocket.API",
                    serviceNamespace: configuration["Telemetry:Environment"] ?? "dev",
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
                        .AddOtlpExporter(o =>
                        {
                            o.Protocol = OtlpExportProtocol.HttpProtobuf;
                            o.Endpoint = new Uri(configuration["Telemetry:Collector"]+"/v1/traces");
                        });
                    })
                    .WithMetrics(metrics =>
                    {
                        metrics
                        .AddMeter(Instrumentation.MeterName)
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation()
                        .AddOtlpExporter(o =>
                        {
                            o.Protocol = OtlpExportProtocol.HttpProtobuf;
                            o.Endpoint = new Uri(configuration["Telemetry:Collector"]+"/v1/metrics");
                        });
                    });
            }

            return services;
        }

    }
}
