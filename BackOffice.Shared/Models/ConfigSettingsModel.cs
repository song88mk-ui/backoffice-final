using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class ConfigSettingsModel
    {
        public ConfigSettingsModel()
        {
            MarketConfig = new List<MarketConfigModel>();
            LeagueConfig = new List<LeagueConfigModel>();
        }
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public int FixtureTypeId { get; set; }
        public Guid ClientId { get; set; }
        public List<MarketConfigModel> MarketConfig { get; set; }
        public List<LeagueConfigModel> LeagueConfig { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
    }

    public class MarketConfigModel
    {
        public MarketConfigModel()
        {
            PrematchSettings = new List<MarketMargin>();
            LivematchSettings = new List<MarketMargin>();
        }
        public string MarketTemplateId { get; set; }
        public bool IsActive { get; set; }
        public List<MarketMargin> PrematchSettings { get; set; }
        public List<MarketMargin> LivematchSettings { get; set; }
    }
    public class LeagueConfigModel
    {
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public bool IsActive { get; set; }
    }
    public class MarketMargin
    {
        public int T { get; set; }
        public int M { get; set; }
    }
}
