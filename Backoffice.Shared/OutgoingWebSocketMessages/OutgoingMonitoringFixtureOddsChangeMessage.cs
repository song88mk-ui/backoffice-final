using Backoffice.Shared.Models.HubMessages;

namespace Backoffice.Shared.OutgoingWebSocketMessages
{
    public class OutgoingMonitoringFixtureOddsChangeMessage : OutgoingBaseMessage
    {
        public OddsChangeMonitoringMessage Pa { get; set; }
    }
}
