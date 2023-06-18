using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ManualBetDetailHistoryRequest
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string ClientIdRefs { get; set; }
        public string Currencies { get; set; }
        public string SportIds { get; set; }
        public string FromStatus { get; set; }
        public string ToStatus { get; set; }
        public string ReferenceIds { get; set; }
        public DateTime FromSettleDate { get; set; }
        public DateTime ToSettleDate { get; set; }
        public string MemberName { get; set; }
        public string SettledBy { get; set; }
    }
}
