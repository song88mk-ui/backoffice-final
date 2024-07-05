using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Utilities;
using Backoffice.Utility.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = services.BuildServiceProvider().GetService<IConfiguration>();

                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddSingleton<IBackofficeRepository, BackofficeRepository>();
                    services.AddHostedService<Worker>();
                });
    }
}
