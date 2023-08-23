using Backoffice.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class BetModifyRequest
    {
        public BetModifyRequest()
        {
            BetIds = new List<string>();
        }
        public List<string> BetIds { get; set; }
        public BetStatus NewStatus { get; set; }
        public string Reason { get; set; }
        public long? ModifiedByUserId { get; set; }
    }
}
