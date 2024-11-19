using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.OutgoingWebSocketMessages
{
    public class OutgoingOddsChangeMessage : OutgoingBaseMessage
    {
        public OddsChangeFixtureMessage Pa { get; set; }
    }
}
