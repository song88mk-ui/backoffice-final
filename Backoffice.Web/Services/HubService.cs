
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.HubMessages;
using Backoffice.Web.States;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

namespace Backoffice.Web.Services
{
    public class HubService : IAsyncDisposable
    {
        public string ConnectionId { get; private set; }

        private HubConnection _hubConnection;
        private List<string> _channels = new List<string>();
        private string _hubUri;
        private bool _isConnecting;
        private UserState _userState;

        public HubService(IConfiguration configuration, UserState userState)
        {
            _hubUri = configuration["API:HubAPI"] ?? throw new ArgumentNullException("API:HubAPI configuration is missing.");
            _userState = userState;
        }

        public async Task ConnectAsync(Func<NotificationsModel, Task>? processNotification = null)
        {
            try
            {
                if (_isConnecting)
                    return;

                if (_hubConnection != null && (_hubConnection.State == HubConnectionState.Connected || _hubConnection.State == HubConnectionState.Connecting))
                    return;

                _isConnecting = true;

                var userId = _userState.Model.Id;
                _hubConnection = new HubConnectionBuilder()
                                .WithUrl($"{_hubUri}?userId={userId}", options =>
                                {
                                    options.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
                                })
                                .AddMessagePackProtocol(options =>
                                {
                                    StaticCompositeResolver.Instance.Register(
                                        ContractlessStandardResolver.Instance
                                    );
                                    options.SerializerOptions = MessagePackSerializerOptions.Standard
                                        .WithResolver(ContractlessStandardResolver.Instance)
                                        .WithSecurity(MessagePackSecurity.UntrustedData);
                                })
                                .WithAutomaticReconnect()
                                .Build();

                _hubConnection.ServerTimeout = TimeSpan.FromSeconds(30);
                _hubConnection.KeepAliveInterval = TimeSpan.FromSeconds(10);

                _hubConnection.Closed += async (error) =>
                {
                    Log.Error($"HubConnection Closed: {error?.Message} - {error?.StackTrace}");
                    await ConnectAsync();
                };

                _hubConnection.Reconnecting += (error) =>
                {
                    Log.Warning($"HubConnection Reconnecting: {error?.Message} - {error?.StackTrace}");
                    return Task.CompletedTask;
                };

                _hubConnection.Reconnected += async (connectionId) =>
                {
                    ConnectionId = connectionId;
                    _userState.ConnectionId = connectionId;

                    var tempChannel = new List<string>(_channels);
                    _channels.Clear();

                    foreach (var channel in tempChannel)
                    {
                        await SubscribeToChannel(channel);
                    }

                    await SubscribeToChannel("onbetstopglobal");
                };

                await _hubConnection.StartAsync();
                ConnectionId = _hubConnection.ConnectionId;
                _userState.ConnectionId = _hubConnection.ConnectionId;
                await SubscribeToChannel("onbetstopglobal");

                if (processNotification != null)
                    await OnNotificationReceived(processNotification);
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message} - {ex.StackTrace}");
            }
            _isConnecting = false;
        }
        public async Task OnNotificationReceived(Func<NotificationsModel, Task> processNotification)
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
                return;

            _hubConnection.On("OnNotification", async (NotificationsModel message) => await processNotification(message));
            await Task.CompletedTask;
        }

        public async Task SubscribeToChannel(string key)
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            {
                _channels.Add(key);
                return;
            }

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
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            {
                _channels.Remove(key);
                return;
            }

            if (!_channels.Contains(key))
                return;

            await _hubConnection.InvokeAsync(
                "UnSubscribeToChannel",
                key
            );

            _channels.Remove(key);
        }

        public async Task RegisterMessageHandlers(Func<BetStopFixtureMessage, Task> processOnBetStopFixture, Func<BetStopGlobalMessage, Task> processOnBetStopGlobal, Func<OddsChangeMonitoringMessage, Task> processOddsChangeMonitoring = null, Func<OddsChangeFixtureDetailsMessage, Task> processOddsChange = null)
        {
            if(_hubConnection == null) return;

            if (processOddsChangeMonitoring != null)
                _hubConnection.On<OddsChangeMonitoringMessage>("OnMonitoringOddsChange", async message => await processOddsChangeMonitoring(message));

            if (processOddsChange != null)
                _hubConnection.On<OddsChangeFixtureDetailsMessage>("OnOddsChange", async message =>
                {
                    Console.WriteLine(" - MessageReceived");
                    await processOddsChange(message);
                });

            _hubConnection.On<byte>("OnOddsChange", async message =>
            {
                Console.WriteLine(" - MessageReceived (Byte)");
            });

            _hubConnection.On<BetStopFixtureMessage>("OnBetStopFixture", async message
                => await processOnBetStopFixture(message));

            _hubConnection.On<BetStopGlobalMessage>("OnBetStopGlobal", async message
                => await processOnBetStopGlobal(message));

            await Task.CompletedTask;
        }
        public async ValueTask DisposeAsync()
        {
            if(_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();

                Log.Warning("HubConnection Disposed");
            }
        }
    }
}
