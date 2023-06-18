using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class ConfigModel
    {
        public List<ClientMarketConfig> ClientMarket { get; set; }
        public List<ClientLeagueConfig> ClientLeague { get; set; }
    }

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

    public class ClientPrematchMarketConfig
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
        public string Time { get; set; }
    }

    public class ClientLiveMarketConfig
    {
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public string MarketTemplateId { get; set; }
        public int FixtureTypeId { get; set; }
        public Guid ClientId { get; set; }
        public bool IsActive { get; set; }
        public string Live { get; set; }
        public string Time { get; set; }
    }
}
