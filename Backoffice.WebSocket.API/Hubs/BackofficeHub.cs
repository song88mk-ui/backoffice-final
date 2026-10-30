using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.OutgoingWebSocketMessages;
using Backoffice.WebSocket.API.Events;
using Backoffice.WebSocket.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace Backoffice.WebSocket.API.Hubs
{
    [Authorize(Policy = HubPolicies.Connect)]
    public class BackofficeHub : Hub<IBackofficeEvent>
    {
        private static readonly Dictionary<string, string> _users = new();

        private readonly HubSecurityOptions _security;

        public BackofficeHub(HubSecurityOptions security)
        {
            _security = security;
        }

        public override Task OnConnectedAsync()
        {
            // The user id comes from the validated Auth token (BO-131). The ?userId= query value is
            // only honoured in log-only mode, for Web pods that do not send the token yet.
            string userId = Context.User?.FindFirst(HubClaims.UserId)?.Value;
            if (string.IsNullOrEmpty(userId) && !_security.Enforce)
                userId = Context.GetHttpContext()?.Request.Query["userId"];

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
        [Authorize(Policy = HubPolicies.Publish)]
        public async Task PublishMonitoringOddsChange(OutgoingMonitoringFixtureOddsChangeMessage payload) => await Clients.Groups(payload.Gr).OnMonitoringOddsChange(payload.Pa);
        [Authorize(Policy = HubPolicies.Publish)]
        public async Task PublishOddsChange(OutgoingFixtureDetailsOddsChangeMessage payload) => await Clients.Groups(payload.Gr).OnOddsChange(payload.Pa);
        [Authorize(Policy = HubPolicies.Publish)]
        public async Task PublishBetStopFixture(OutgoingBetStopFixtureMessage payload) => await Clients.Groups(payload.Gr).OnBetStopFixture(payload.Pa);
        [Authorize(Policy = HubPolicies.Publish)]
        public async Task PublishBetStopGlobal(OutgoingBetStopGlobalMessage payload) => await Clients.Groups($"onbetstopglobal").OnBetStopGlobal(payload.Pa);
        [Authorize(Policy = HubPolicies.Publish)]
        public async Task PublishNotification(OutgoingNotificationMessage payload)
        {
            if (!string.IsNullOrEmpty(payload.UsId))
            {
                string connectionId;
                lock (_users)
                {
                    _users.TryGetValue(payload.UsId, out connectionId);
                }

                // A notification for one user who is not connected is dropped, never sent to everyone.
                if (connectionId != null)
                    await Clients.Client(connectionId).OnNotification(payload.Pa);
                return;
            }

            var connectionEvent = payload.Gr != null ? Clients.Groups(payload.Gr) : Clients.All;
            await connectionEvent.OnNotification(payload.Pa);
        }
    }
}
