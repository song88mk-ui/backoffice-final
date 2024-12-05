using Backoffice.Shared.Models.HubMessages;

namespace Backoffice.Shared.OutgoingWebSocketMessages
{
    public class OutgoingFixtureDetailsOddsChangeMessage : OutgoingBaseMessage
    {
        public OddsChangeFixtureDetailsMessage Pa { get; set; }
    }
}
