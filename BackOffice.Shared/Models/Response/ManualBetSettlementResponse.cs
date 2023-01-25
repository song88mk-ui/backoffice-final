using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class ManualBetSettlementResponse
    {
        #region Properties
        public List<ManualBetResponse> Data { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
        #endregion

        #region Ctor
        public ManualBetSettlementResponse()
        {
            Data = new List<ManualBetResponse>();
        }
        #endregion        
    }
}
