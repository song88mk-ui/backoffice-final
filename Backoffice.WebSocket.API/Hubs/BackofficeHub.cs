using Backoffice.WebSocket.API.Events;
using Microsoft.AspNetCore.SignalR;

namespace Backoffice.WebSocket.API.Hubs
{
    public class BackofficeHub : Hub<IBackofficeEvent>
    {
    }
}
