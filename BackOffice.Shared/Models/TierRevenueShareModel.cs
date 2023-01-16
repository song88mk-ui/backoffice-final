
using System.Collections.Generic;

namespace Backoffice.Shared.Models
{
    public class TierRevenueShareModel
    {
        public TierRevenueShareModel()
        {
            Tiers = new List<TierModel>();
        }

        public decimal AboveTierPercentage { get; set; }
        public List<TierModel> Tiers { get; set; }

        public class TierModel
        {
            public int Tier { get; set; }
            public decimal From { get; set; }
            public decimal To { get; set; }
            public decimal Percentage { get; set; }
        }
    }
}
