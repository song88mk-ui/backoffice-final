using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.OutgoingWebSocketMessages;
using Backoffice.WebSocket.API.Events;
using Microsoft.AspNetCore.SignalR;

namespace Backoffice.WebSocket.API.Hubs
{
    public class BackofficeHub : Hub<IBackofficeEvent>
    {
        public async Task SubscribeToChannel(string request) => await Groups.AddToGroupAsync(Context.ConnectionId, request.ToLower());
        public async Task UnSubscribeToChannel(string request) => await Groups.RemoveFromGroupAsync(Context.ConnectionId, request.ToLower());
        public async Task PublishOddsChange(OutgoingOddsChange payload) => await Clients.Groups(payload.Gr).OnOddsChange(payload.Pa);
        //public async Task PublishNotification(string gr) => await Clients.Groups(gr);
    }
}
