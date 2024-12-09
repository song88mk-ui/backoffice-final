using Backoffice.Shared.IncomingSbEngineMessages;
using Backoffice.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;

namespace Backoffice.WebSocket.Service.Repositories
{
    public interface ISportsbookRepository
    {
        Task ProcessMarketTemplatePlayerProps();
        Task ProcessClientsWithoutMargin();
        Task ProcessOnMonitoringOddsChange(IncomingOddsChangeMessage message, HubConnection hubConnection);
        Task ProcessOnMonitoringDetailsOddsChange(IncomingOddsChangeMessage message, HubConnection hubConnection);
        Task ProcessOnBetStopFixture(IncomingBetStopFixtureMessage message, HubConnection hubConnection);
        Task ProcessOnBetStopGlobal(IncomingBetStopGlobalMessage message, HubConnection hubConnection);
        Task ProcessNotification(NotificationsDetailsModel message, HubConnection hubConnection);
    }
}
