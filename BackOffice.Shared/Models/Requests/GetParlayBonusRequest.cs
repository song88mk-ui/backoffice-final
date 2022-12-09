using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class GetParlayBonusRequest
    {
        public long ClientIdRef { get; set; }
        public int SelectionCount { get; set; }
    }
}
