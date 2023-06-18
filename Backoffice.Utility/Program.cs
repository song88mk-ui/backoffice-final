using Backoffice.Shared.Utilities;
using Backoffice.Utility.Repository;
using Backoffice.Utility.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System;
using System.Reflection;

namespace Backoffice.Utility
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = services.BuildServiceProvider().GetService<IConfiguration>();

                    Action<ResourceBuilder> resource = r => r.AddService(
                        serviceName: "BackOffice.Utility",
                        serviceNamespace: configuration["Telemetry:Environment"],
                        serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "latest",
                        serviceInstanceId: Environment.MachineName);

                    services.AddSingleton<Instrumentation>();

                    if (Convert.ToBoolean(configuration["Telemetry:Enabled"]))
                    {
                      services.AddOpenTelemetry()
                        .ConfigureResource(resource)
                        .WithTracing(tracing =>
                        {
                            tracing
                              .AddSource(Instrumentation.ActivitySourceName)
                              .SetSampler(new AlwaysOnSampler())
                              .AddHttpClientInstrumentation()
                              .AddSqlClientInstrumentation()
                              .AddOtlpExporter(opts =>
                              {
                                opts.Protocol = OtlpExportProtocol.HttpProtobuf;
                                opts.Endpoint = new Uri(configuration["Telemetry:Collector"]+"/v1/traces");
                              });
                        })
                        .WithMetrics(metrics =>
                        {
                            metrics
                              .AddMeter(Instrumentation.MeterName)
                              .AddRuntimeInstrumentation()
                              .AddHttpClientInstrumentation()
                              .AddOtlpExporter(opts =>
                              {
                                opts.Protocol = OtlpExportProtocol.HttpProtobuf;
                                opts.Endpoint = new Uri(configuration["Telemetry:Collector"]+"/v1/metrics");
                              });
                        });
                    }

                    services.AddSingleton<IBackofficeRepository, BackofficeRepository>();
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddHostedService<Worker>();
                }).UseWindowsService();
    }
}
