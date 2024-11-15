
using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backoffice.Web.Services
{
    public class HubService : IAsyncDisposable
    {
        private HubConnection _hubConnection { get; set; }
        public string ConnectionId {  get; private set; }

        public HubService(IConfiguration configuration)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(new Uri(configuration["API:HubAPI"]))
                .WithAutomaticReconnect()
            .Build();

            _hubConnection.StartAsync().ContinueWith(task =>
            {
                ConnectionId = string.Empty;
                if (task.IsFaulted)
                {
                    //_logger.LogError("Error Connecting to hub\", \"Fault in connecting.");
                }
                else if (task.IsCompletedSuccessfully)
                {
                    ConnectionId = _hubConnection?.ConnectionId?.ToString() ?? string.Empty;
                }
            });
        }
        public async Task OnNotificationReceived(Func<NotificationsModel, Task> processNotification)
            => _hubConnection.On("Notification", async (NotificationsModel message) => await processNotification(message));

        public async Task InitializeOeMessageHandlers(
            Func<OnExternalOddsChangeMessage, Task> processOddsChangeMonitoring,
            Func<OnExternalBetStopFixtureMessage, Task> processOnBetStopFixture,
            Func<OnExternalBetStopGlobalMessage, Task> processOnBetStopGlobal)
        {
            _hubConnection.On<OnExternalOddsChangeMessage>("onoddschange", async message =>
            {
                await processOddsChangeMonitoring(message);
            });

            _hubConnection.On<OnExternalBetStopFixtureMessage>("onbetstopfixture", async message =>
            {
                await processOnBetStopFixture(message);
            });

            _hubConnection.On<OnExternalBetStopGlobalMessage>("onbetstopglobal", async message =>
            {
                await processOnBetStopGlobal(message);
            });
        }
        public async ValueTask DisposeAsync()
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
        }
    }
}
