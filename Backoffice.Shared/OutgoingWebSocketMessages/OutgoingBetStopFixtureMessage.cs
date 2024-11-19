using Backoffice.Shared.Models.HubMessages;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Backoffice.Shared.OutgoingWebSocketMessages
{
    public class OutgoingBetStopFixtureMessage : OutgoingBaseMessage
    {
        public BetStopFixtureMessage Pa { get; set; }
    }
}
