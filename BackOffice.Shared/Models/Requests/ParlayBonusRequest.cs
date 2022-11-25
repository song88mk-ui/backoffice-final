using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ParlayBonusRequest
    {
        public int ClientIdRef { get; set; }
        public decimal ParlayBonusMinOdds { get; set; }
        public List<ParlayBonusConfigRequest> ParlayBonusConfig { get; set; }
    }
}
