using Backoffice.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    // BO-133: an import interrupted by a restart left its batch Running, which locked Client Import/Update
    // for the environment until someone edited the database. On startup, close those batches as Interrupted.
    // Runs after startup so a slow or unreachable database never delays the API becoming ready.
    public class ClientImportRecoveryService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ClientImportRecoveryService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IClientImportRepository>();

                var result = await repository.InterruptAbandonedBatches();
                if (result.StatusCode != StatusCode.Success)
                {
                    Log.Warning("Client import recovery skipped: {Message}", result.Message);
                }
                else if (result.Result > 0)
                {
                    Log.Warning("Client import recovery marked {Count} batch(es) left Running as Interrupted", result.Result);
                }
            }
            catch (Exception ex)
            {
                // The stale limit in IsImportRunning still frees the lock later.
                Log.Error(ex, "Client import recovery failed");
            }
        }
    }
}
