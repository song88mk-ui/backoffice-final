using Backoffice.Shared.OutgoingEngineMessages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class MessageSettlementRequest
    {
        public MessageSettlementIodocsRequest MessageSettlementIodocsRequest { get; set; }
        public OnActivityLogMessage OnActivityLogMessage { get; set; }
    }

    public class MessageSettlementIodocsRequest
    {
        public string Product { get; set; }
        public string FixtureSpecifier { get; set; }
        public int RequestId { get; set; }
        public int NodeId { get; set; }
    }
}
