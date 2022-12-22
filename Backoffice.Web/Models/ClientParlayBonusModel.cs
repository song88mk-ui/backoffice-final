namespace Backoffice.Web.Models
{
    public class ClientParlayBonusModel
    {
        public ClientParlayBonusModel()
        {
            ParlayBonusConfig = new List<ParlayBonusConfig>();
        }
        public long ClientIdRef { get; set; }
        public decimal ParlayBonusMinOdds { get; set; }
        public List<ParlayBonusConfig> ParlayBonusConfig { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPage { get; set; }
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
