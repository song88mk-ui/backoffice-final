using System;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class ClientMarketConfig
    {
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public string MarketTemplateId { get; set; }
        public int FixtureTypeId { get; set; }
        public Guid ClientId { get; set; }
        public bool IsActive { get; set; }
        public string Prematch { get; set; }
        public string Live { get; set; }
    }

    public class ClientLeagueConfig
    {
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public Guid ClientId { get; set; }
        public bool IsActive { get; set; }
    }

}
