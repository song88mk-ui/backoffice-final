
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System.Threading.Tasks;

namespace Backoffice.Web.Services
{
    public class HubService : IAsyncDisposable
    {
        private HubConnection _hubConnection { get; set; }
        public string ConnectionId { get; private set; }

        public HubService(IConfiguration configuration)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(new Uri(configuration["API:HubAPI"]))
                .WithAutomaticReconnect()
            .Build();

            _hubConnection.Closed += async (error) =>
            {
                Log.Error($"HubConnection Closed: {error?.Message} - {error?.StackTrace}");
            };

            _hubConnection.Reconnecting += (error) =>
            {
                Log.Warning($"HubConnection Reconnecting: {error?.Message} - {error?.StackTrace}");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += async (connectionId) =>
            {
                var tempChannel = new List<string>(_channels);
                _channels.Clear();

                foreach (var channel in tempChannel)
                {
                    await SubscribeToChannel(channel);
                }

                await SubscribeToChannel("onbetstopglobal");
                Log.Information($"HubConnection Reconnected with Connection ID: {connectionId}");
            };

            try
            {
                _hubConnection.StartAsync().ContinueWith(async task =>
                {
                    if (task.IsFaulted)
                    {
                        Log.Error($"HubConnection failed to connect: {task?.Exception?.Message} - {task?.Exception?.StackTrace}");
                    }
                    else if (task.IsCompletedSuccessfully)
                    {
                        var connectionId = _hubConnection.ConnectionId?.ToString();
                        Log.Information($"HubConnection Connected with Connection ID: {connectionId}");

                        await SubscribeToChannel("onbetstopglobal");
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message} - {ex.StackTrace}");
            }
        }
        public async Task OnNotificationReceived(Func<NotificationsModel, Task> processNotification)
        {
            if (_hubConnection.State != HubConnectionState.Connected)
                return;

            _hubConnection.On("Notification", async (NotificationsModel message) => await processNotification(message));
        }

        private List<string> _channels = new List<string>();
        public async Task SubscribeToChannel(string key)
        {
            if (_hubConnection.State != HubConnectionState.Connected)
                return;

            if (_channels.Contains(key))
                return;

            await _hubConnection.InvokeAsync(
                "SubscribeToChannel",
                key
            );
            _channels.Add(key);
        }
        public async Task UnSubscribeToChannel(string key)
        {
            if (_hubConnection.State != HubConnectionState.Connected)
                return;

            if (!_channels.Contains(key))
                return;

            await _hubConnection.InvokeAsync(
                "UnSubscribeToChannel",
                key
            );

            _channels.Remove(key);
        }

        public async Task InitializeOeMessageHandlers(
            Func<OddsChangeFixtureMessage, Task> processOddsChangeMonitoring,
            Func<BetStopFixtureMessage, Task> processOnBetStopFixture,
            Func<BetStopGlobalMessage, Task> processOnBetStopGlobal)
        {
            _hubConnection.On<OddsChangeFixtureMessage>("OnOddsChange", async message
                => await processOddsChangeMonitoring(message));

            _hubConnection.On<BetStopFixtureMessage>("OnBetStopFixture", async message
                => await processOnBetStopFixture(message));

            _hubConnection.On<BetStopGlobalMessage>("OnBetStopGlobal", async message
                => await processOnBetStopGlobal(message));
        }
        public async ValueTask DisposeAsync()
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
        }
    }
}
