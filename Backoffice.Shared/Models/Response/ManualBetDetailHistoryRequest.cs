using System;
using System.Collections.Generic;
using System.Text;
using static Backoffice.Shared.Models.ManualSettlementBetHistoryModel;

namespace Backoffice.Shared.Models.Response
{
    public class ManualSettlementBetHistoryResponse
    {
        public ManualSettlementBetHistoryResponse()
        {
            Bets = new List<ManualSettlementBet>();
        }
        public int CurrentPage { get; set; }
        public int PageCount { get; set; }
        public int PageSize { get; set; }
        public int RowCount { get; set; }
        public List<ManualSettlementBet> Bets { get; set; }
    }
}
