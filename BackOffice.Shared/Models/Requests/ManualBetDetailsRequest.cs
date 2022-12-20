using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ManualBetDetailsRequest
    {
        public ManualBetDetailsRequest()
        {
            BetIds = new List<string>();
        }
        public List<string> BetIds { get; set; }
    }
}
