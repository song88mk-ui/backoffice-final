using Backoffice.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    public class OnGetManualBetSettlementHistoryMessage
    {
        public ManualBetSettlementCallbackModel ManualBetSettlementCallbackModel { get; set; }
        public string ConnectionId { get; set; }
    }
}
