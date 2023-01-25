using Backoffice.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ManualBetSettlementRequest
    {
        #region Properties
        public List<ManualBetRequest> BetData { get; set; }
        public long? ModifiedByUserId { get; set; }
        #endregion

        #region Ctor
        public ManualBetSettlementRequest()
        {
            BetData = new List<ManualBetRequest>();
        }
        #endregion
    }
}
