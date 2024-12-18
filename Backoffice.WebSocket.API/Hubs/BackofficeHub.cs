using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.OutgoingWebSocketMessages;
using Backoffice.WebSocket.API.Events;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace Backoffice.WebSocket.API.Hubs
{
    public class BackofficeHub : Hub<IBackofficeEvent>
    {
        private static readonly Dictionary<string, string> _users = new();

        public override Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"];

            if (!string.IsNullOrEmpty(userId))
            {
                lock (_users)
                {
                    _users[userId] = Context.ConnectionId;
                }
            }

            return base.OnConnectedAsync();
        }
        public override Task OnDisconnectedAsync(Exception exception)
        {
            var userId = _users.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;

            if (!string.IsNullOrEmpty(userId))
            {
                lock (_users)
                {
                    _users.Remove(userId);
                }
            }

            return base.OnDisconnectedAsync(exception);
        }
        public async Task SubscribeToChannel(string request) => await Groups.AddToGroupAsync(Context.ConnectionId, request.ToLower());
        public async Task UnSubscribeToChannel(string request) => await Groups.RemoveFromGroupAsync(Context.ConnectionId, request.ToLower());
        public async Task PublishMonitoringOddsChange(OutgoingMonitoringFixtureOddsChangeMessage payload) => await Clients.Groups(payload.Gr).OnMonitoringOddsChange(payload.Pa);
        public async Task PublishOddsChange(OutgoingFixtureDetailsOddsChangeMessage payload) => await Clients.Groups(payload.Gr).OnOddsChange(payload.Pa);
        public async Task PublishBetStopFixture(OutgoingBetStopFixtureMessage payload) => await Clients.Groups(payload.Gr).OnBetStopFixture(payload.Pa);
        public async Task PublishBetStopGlobal(OutgoingBetStopGlobalMessage payload) => await Clients.Groups($"onbetstopglobal").OnBetStopGlobal(payload.Pa);
        public async Task PublishNotification(OutgoingNotificationMessage payload)
        {
            var connectionEvent = Clients.All;

            if (!string.IsNullOrEmpty(payload.UsId) && _users.TryGetValue(payload.UsId, out var connectionId))
            {
                connectionEvent = Clients.Client(connectionId);
            }
            else if (payload.Gr != null)
                connectionEvent = Clients.Groups(payload.Gr);

            await connectionEvent.OnNotification(payload.Pa);
        }
    }
}
