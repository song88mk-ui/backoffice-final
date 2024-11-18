using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.OutgoingWebSocketMessages
{
    public class OutgoingOddsChange
    {
        public List<string> Gr { get; set; } = new List<string>();
        public OnExternalOddsChangeMessage Pa { get; set; }
    }
}
