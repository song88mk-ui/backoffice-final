using Backoffice.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using static Backoffice.Shared.Models.Requests.AddOrUpdateBetConfigRequest;
using static Backoffice.Shared.Models.Response.ClientListResponse;

namespace Backoffice.Shared.Models.Response
{
    public class BetConfigResponse
    {
        public BetConfigResponse()
        {

        }
        public DefaultSportConfigFE DefaultSportConfig { get; set; }
        public List<OverrideSportConfigFE> OverrideSportConfig { get; set; }
        public ParlayConfigFE Parlayconfig { get; set; }
        public TaxConfig TaxConfig { get; set; }
        public PriceboostBetSetting PriceboostConfig { get; set; }
        public BetBuilderBetSetting BetBuilderConfig { get; set; }

        public class OverrideSportConfigFE
        {
            public int SportId { get; set; }
            public string SportName { get; set; }
            public decimal MinStake { get; set; }
            public decimal MaxStake { get; set; }
            public decimal MaxPerGame { get; set; }
        }

        public class DefaultSportConfigFE
        {
            public decimal MinStake { get; set; }
            public decimal MaxStake { get; set; }
            public decimal MaxPerGame { get; set; }
        }

        public class ParlayConfigFE
        {
            public ParlayConfigFE()
            {
                ParlayBonusConfig = new List<ParlayBonusConfigFE>();
            }
            public decimal MinStake { get; set; }
            public decimal MaxStake { get; set; }
            public decimal MaxPayout { get; set; }
            public decimal ParlayBonusMaxWinnings { get; set; }
            public decimal ParlayBonusMinOdds { get; set; }
            public List<ParlayBonusConfigFE> ParlayBonusConfig { get; set; }
        }

        public class ParlayBonusConfigFE
        {
            public int SelectionCount { get; set; }
            public decimal ParlayBonus { get; set; }
        }

    }
}
