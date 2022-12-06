using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class GetParlayBonusRequest
    {
        public List<long> ClientIdRef { get; set; }
        public int SelectionCount { get; set; }
    }
}
