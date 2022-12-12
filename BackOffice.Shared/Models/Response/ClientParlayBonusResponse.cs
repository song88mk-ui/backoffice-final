using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class ClientParlayBonusResponse
    {
        public ClientParlayBonusResponse()
        {
            ParlayBonusConfig = new List<ParlayBonusConfig>();
        }
        public long ClientIdRef { get; set; }
        public decimal ParlayBonusMinOdds { get; set; }
        public List<ParlayBonusConfig> ParlayBonusConfig { get; set; }
    }

    public class ParlayBonusConfig
    {
        public ParlayBonusConfig()
        {
            ParlayBonusHistory = new List<ParlayBonusHistory>();
        }
        public int SelectionCount { get; set; }
        public decimal ParlayBonus { get; set; }
        public List<ParlayBonusHistory> ParlayBonusHistory { get; set; }

    }

    public class ParlayBonusHistory
    {
        public string LastUpdate { get; set; }
        public decimal ParlayBonus { get; set; }
    }
}
