using Backoffice.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class ManualBetSettlementModel
    {
        #region Properties
        public List<ManualBetData> BetData { get; set; }
        public long? ModifiedByUserId { get; set; }
        #endregion

        #region Ctor
        public ManualBetSettlementModel()
        {
            BetData = new List<ManualBetData>();
        }
        #endregion        

        #region Nested Class
        public class ManualBetData
        {
            public string BetId { get; set; }
            public BetStatus NewStatus { get; set; }
            public string Reason { get; set; }
        }
        #endregion
    }
}
