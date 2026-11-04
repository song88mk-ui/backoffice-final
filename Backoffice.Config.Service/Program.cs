using Backoffice.Config.Service.Repository;
using Backoffice.Config.Service.Telemetry;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Backoffice.Config.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((context, _, serilog) =>
                {
                    serilog.ReadFrom.Configuration(context.Configuration);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<Instrumentation>();
                    services.AddTelemetry(new TelemetrySettings(
                        ServiceName: Instrumentation.ActivitySourceName,
                        ActivitySourceName: Instrumentation.ActivitySourceName,
                        MeterName: Instrumentation.MeterName));

                    // a factory, not one shared context: the singleton repository creates a context per message (BO-138)
                    services.AddDbContextFactory<BackofficeContext>(options => options.UseSqlServer(hostContext.Configuration["ConnectionStrings:Backoffice"]));
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddSingleton<IBackofficeRepository, BackofficeRepository>();
                    services.AddHostedService<Worker>();
                });
    }
}
