using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    public class OnGetCancelBetHistoryMessage : ActivityLogRequest
    {
        public string ConnectionId { get; set; }
    }
}
