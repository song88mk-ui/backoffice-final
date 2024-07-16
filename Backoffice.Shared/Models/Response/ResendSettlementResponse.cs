using Backoffice.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class ResendSettlementResponse
    {
        public ResendSettlementResponse()
        {
            Data = new List<ResendSettlementResponseData>();
        }
        public List<ResendSettlementResponseData> Data { get; set; }
        #region Nested Class
        public class ResendSettlementResponseData
        {
            public Guid BetId { get; set; }
            public StatusCode StatusCode { get; set; }
            public string Message { get; set; }
        }
        #endregion
    }
}
