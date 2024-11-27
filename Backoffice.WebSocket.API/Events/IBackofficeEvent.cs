using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;

namespace Backoffice.WebSocket.API.Events
{
    public interface IBackofficeEvent
    {
        Task OnOddsChange(OddsChangeFixtureDetailsMessage message);
        Task OnMonitoringOddsChange(OddsChangeMonitoringMessage message);
        Task OnBetStopFixture(BetStopFixtureMessage message);
        Task OnBetStopGlobal(BetStopGlobalMessage message);
    }
}
