using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class ManualBetResponse
    {
        public ManualBetDetailsModel BetDetails { get; set; }
        public string OldBetStatus { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
    }
}
