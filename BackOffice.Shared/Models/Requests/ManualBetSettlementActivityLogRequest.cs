using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ManualBetSettlementActivityLogRequest
    {
        public ManualBetSettlementCallbackModel ManualBetSettlementCallbackModel { get; set; }
        public MetaData MetaData { get; set; }
    }
}
