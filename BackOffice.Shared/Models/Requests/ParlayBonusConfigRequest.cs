using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ParlayBonusConfigRequest
    {
        public int SelectionCount { get; set; }
        public decimal ParlayBonus { get; set; }
    }
}
