using Backoffice.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    // BO-159: the outcome of copying one client's market sort order to several clients, one entry per target.
    public class CopyClientMarketSortOrderResponse
    {
        public List<CopyClientMarketSortOrderTargetResult> Targets { get; set; } = new List<CopyClientMarketSortOrderTargetResult>();
    }

    public class CopyClientMarketSortOrderTargetResult
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public StatusCode StatusCode { get; set; }
        public string Message { get; set; }
    }
}
