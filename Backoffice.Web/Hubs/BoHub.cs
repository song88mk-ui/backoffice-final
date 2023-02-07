using Microsoft.AspNetCore.SignalR;

namespace Backoffice.Web.Hubs
{
    public class BoHub : Hub
    {
        public async Task SendMessage(string method, string message)
        {
            await Clients.All.SendAsync(method, message);
        }
        public async Task SendMessageToUser(string method, string connectionId, string message)
        {
            await Clients.Client(connectionId).SendAsync(method, message);
        }

        //public async Task OnConfigChange(ConfigSettingsModel message)
        //{
        //    await Clients.All.SendAsync("onconfigchange", message);
        //}
    }
}
