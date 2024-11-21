using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.OutgoingWebSocketMessages;
using Backoffice.WebSocket.API.Events;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace Backoffice.WebSocket.API.Hubs
{
    public class BackofficeHub : Hub<IBackofficeEvent>
    {
        public async Task SubscribeToChannel(string request) => await Groups.AddToGroupAsync(Context.ConnectionId, request.ToLower());
        public async Task UnSubscribeToChannel(string request) => await Groups.RemoveFromGroupAsync(Context.ConnectionId, request.ToLower());
        public async Task PublishOddsChange(OutgoingOddsChangeMessage payload) => await Clients.Groups(payload.Gr).OnOddsChange(payload.Pa);
        public async Task PublishBetStopFixture(OutgoingBetStopFixtureMessage payload) => await Clients.Groups(payload.Gr).OnBetStopFixture(payload.Pa);
        public async Task PublishBetStopGlobal(OutgoingBetStopGlobalMessage payload) => await Clients.Groups($"onbetstopglobal").OnBetStopGlobal(payload.Pa);
        //public async Task PublishNotification(string gr) => await Clients.Groups(gr);
    }
}
