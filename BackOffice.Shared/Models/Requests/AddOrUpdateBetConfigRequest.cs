using Backoffice.Shared.Models.Response;
using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class AddOrUpdateBetConfigRequest
    {
        public AddOrUpdateBetConfigRequest() {
            BetConfigPayload = new List<BetConfig>();
        } 
        public Guid ClientId { get; set; }
        public List<BetConfig> BetConfigPayload { get; set; }

        public class BetConfig
        {
            public string CurrencyCode { get; set; }
            public FootballBetSetting Football { get; set; }
            public AnySportBetSetting OtherSports { get; set; }
            public ParlayBetSetting Parlay { get; set; }
            public CricketBetSetting Cricket { get; set; }
            public PriceboostBetSetting Priceboost { get; set; }
            public BetBuilderBetSetting BetBuilder { get; set; }
            public ClientListResponse.TaxConfig TaxConfig { get; set; }
        }
        public class FootballBetSetting
        {
            public decimal MinBet { get; set; }
            public decimal MaxBet { get; set; }
            public decimal MaxFixtureBet { get; set; }
        }

        public class AnySportBetSetting
        {
            public decimal MinBet { get; set; }

            public decimal MaxBet { get; set; }

            public decimal MaxFixtureBet { get; set; }
        }

        public class ParlayBetSetting
        {
            public decimal MinBet { get; set; }

            public decimal MaxBet { get; set; }

            public decimal MaxPayout { get; set; }
            public decimal ParlayBonusMaxWinnings { get; set; }
            public decimal ParlayBonusMinOdds { get; set; }
            public List<ParlayBonusConfig> ParlayBonusConfig { get; set; }
        }

        public class CricketBetSetting
        {
            public decimal MinBet { get; set; }
            public decimal MaxBet { get; set; }
            public decimal MaxFixtureBet { get; set; }
        }

        public class PriceboostBetSetting
        {
            public decimal MaxStake { get; set; }
        }

        public class BetBuilderBetSetting
        {
            public decimal MinStake { get; set; }
            public decimal MaxStake { get; set; }
        }

    }
}
