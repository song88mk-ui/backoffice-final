using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;

namespace Backoffice.WebSocket.API.Events
{
    public interface IBackofficeEvent
    {
        Task OnOddsChange(OddsChangeFixture message);
    }
}
