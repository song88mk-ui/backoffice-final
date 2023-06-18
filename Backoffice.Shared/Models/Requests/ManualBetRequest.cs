using Backoffice.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ManualBetRequest
    {
        public string BetId { get; set; }
        public BetStatus NewStatus { get; set; }
        public string Reason { get; set; }
    }
}
