using Backoffice.Shared.Utilities;
using Backoffice.Utility.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Backoffice.Utility
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "Backoffce Utility Service";
            CreateHostBuilder(args).Build().Run();
            Console.Title = "Backoffce Utility Service";
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IBackofficeRepository, BackofficeRepository>();
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddHostedService<Worker>();
                });
    }
}
