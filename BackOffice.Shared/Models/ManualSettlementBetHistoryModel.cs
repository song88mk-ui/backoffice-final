using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class ManualSettlementBetHistoryModel
    {
        public ManualSettlementBetHistoryModel()
        {
            Bets = new List<ManualSettlementBet>();
        }

        public int CurrentPage { get; set; }
        public int PageCount { get; set; }
        public int PageSize { get; set; }
        public int RowCount { get; set; }
        public List<ManualSettlementBet> Bets { get; set; }

        #region Nested Class
        public class ManualSettlementBet : ManualBetDetailsModel
        {
            public long Id { get; set; }
            public int FromBetStatusId { get; set; }
            public string FromBetStatusName { get; set; }
            public string ToBetStatusId { get; set; }
            public string ToBetStatusName { get; set; }
            public string ManualSettledBy { get; set; }
        }
        #endregion
    }
}
