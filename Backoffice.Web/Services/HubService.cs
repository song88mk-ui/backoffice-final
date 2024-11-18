
using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;
using Backoffice.Web.Models;
using Grpc.Net.Client.Configuration;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using System.Text.Json;
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
                Console.WriteLine("Connection closed. Trying to reconnect...");
            };

            _hubConnection.Reconnecting += (error) =>
            {
                Console.WriteLine("Reconnecting...");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += (connectionId) =>
            {
                Console.WriteLine($"Reconnected with connectionId: {connectionId}");
                return Task.CompletedTask;
            };

            _hubConnection.StartAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Console.WriteLine($"Error Connecting to hub");
                }
                else if (task.IsCompletedSuccessfully)
                {
                    var connectionId = _hubConnection.ConnectionId?.ToString();
                    Console.WriteLine($"Connected with connectionId: {connectionId}");
                }
            });
        }
        public async Task OnNotificationReceived(Func<NotificationsModel, Task> processNotification)
            => _hubConnection.On("Notification", async (NotificationsModel message) => await processNotification(message));

        public async Task SubscribeToChannel(string key) =>
            await _hubConnection.InvokeAsync(
                "SubscribeToChannel",
                key
            );
        public async Task UnSubscribeToChannel(string key) =>
            await _hubConnection.InvokeAsync(
                "UnSubscribeToChannel",
                key
            );

        public async Task InitializeOeMessageHandlers(
            Func<OnExternalOddsChangeMessage, Task> processOddsChangeMonitoring,
            Func<OnExternalBetStopFixtureMessage, Task> processOnBetStopFixture,
            Func<OnExternalBetStopGlobalMessage, Task> processOnBetStopGlobal)
        {
            _hubConnection.On<OnExternalOddsChangeMessage>("OnOddsChange", async message =>
            {
                Console.WriteLine($"OddsChange - {message.SportName,-20} - {message.LeagueId,-10} - {message.LeagueName,-60} - {message.CorrelationId}");
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
