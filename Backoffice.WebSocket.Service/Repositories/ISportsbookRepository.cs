using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Backoffice.Shared.IncomingSbEngineMessages;
using Microsoft.AspNetCore.SignalR.Client;

namespace Backoffice.WebSocket.Service.Repositories
{
    public interface ISportsbookRepository
    {
        Task ProcessMarketTemplatePlayerProps();
        Task ProcessOnMonitoringOddsChange(IncomingOddsChangeMessage message, HubConnection hubConnection);
        Task ProcessOnMonitoringDetailsOddsChange(IncomingOddsChangeMessage message, HubConnection hubConnection);
        Task ProcessOnBetStopFixture(IncomingBetStopFixtureMessage message, HubConnection hubConnection);
        Task ProcessOnBetStopGlobal(IncomingBetStopGlobalMessage message, HubConnection hubConnection);
        Task ProcessNotification(NotificationsDetailsModel message, HubConnection hubConnection);
    }
}
