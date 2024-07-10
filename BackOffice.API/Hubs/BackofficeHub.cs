using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace Backoffice.API.Hubs
{
    public class BackofficeHub : Hub
    {
        static HashSet<string> CurrentConnections = new HashSet<string>();
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            CurrentConnections.Add(connectionId);
            Console.WriteLine($"New device connected: {connectionId}");
            Console.WriteLine("Connected");
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var connection = CurrentConnections.FirstOrDefault(x => x == Context.ConnectionId);

            if (connection != null)
            {
                CurrentConnections.Remove(connection);
            }

            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendAll(string userId, string message)
        {
            await Clients.All.SendAsync("notification", Context.ConnectionId, message);
        }
        public async Task SendPrivateMessage(string userId, string message)
        {
            await Clients.Client(userId).SendAsync("notification", Context.ConnectionId, message);
        }

        public async void GetAllClients()
        {
            await Clients.All.SendAsync("UpdateCount", CurrentConnections);
        }
    }
}
