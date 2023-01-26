using Backoffice.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ResendSettlementRequest
    {
        public ResendSettlementRequest()
        {
            Bets = new List<ResendSettlementRequestData>();
        }

        public List<ResendSettlementRequestData> Bets { get; set; }
        #region Nested Class
        public class ResendSettlementRequestData
        {
            public string BetId { get; set; }
            public BetStatus CurrentStatus { get; set; }
        }
        #endregion
    }
}
