using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class ActivityLogListRequest
    {
        public List<ActivityLogModel> ActivityLogs { get; set; }
    }
}
