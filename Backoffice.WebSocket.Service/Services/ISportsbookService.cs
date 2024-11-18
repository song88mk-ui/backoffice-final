using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;

namespace Backoffice.WebSocket.Service.Services
{
    public interface ISportsbookService
    {
        Task ProcessOnMonitoringOddsChange(OnExternalOddsChangeMessage message, HubConnection hubConnection);
        Task ProcessOnMonitoringDetailsOddsChange(OnExternalOddsChangeMessage message, HubConnection hubConnection);
        Task ProcessNotification(NotificationsDetailsModel message, HubConnection hubConnection);
    }
}
