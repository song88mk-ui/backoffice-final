using System;
using System.Collections.Generic;
using System.Text;
using static Backoffice.Shared.Models.Response.FastOddsFixturesResponse;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;

namespace Backoffice.Shared.Models
{
    public class MockDataFixtureModel
    {
        public MockDataFixtureModel()
        {
            Markets = new List<MarketModel>();
        }

        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string ProviderFixtureId { get; set; }
        public string FixtureId { get; set; }
        public string FixtureName { get; set; }
        public string HomeName { get; set; }
        public string AwayName { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public bool IsLive { get; set; }
        public bool IsLiveStream { get; set; }
        public bool IsSuspended { get; set; }
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
        public int MarketCount { get; set; }
        public string SMSCode { get; set; }
        public string StatisticsURL { get; set; }
        public string Score { get; set; }
        public string CurrentPeriod { get; set; }
        public string CurrentMinute { get; set; }
        public List<int> BetStop { get; set; }
        public bool IsBetStopGlobal { get; set; }
        public int SortOrder { get; set; }
        public int FixtureTypeId { get; set; }
        public SportMarketTemplateGroup MarketTemplateGroups { get; set; }
        public List<MarketModel> Markets { get; set; }
    }

    public class SportMarketTemplateGroup
    {
        public SportMarketTemplateGroup()
        {
            Prematch = new List<MarketTemplateGroup>();
            Live = new List<MarketTemplateGroup>();
        }
        public int SportId { get; set; }
        public List<MarketTemplateGroup> Prematch { get; set; }
        public List<MarketTemplateGroup> Live { get; set; }
    }

    public class MarketTemplateGroup
    {
        public int MarketTemplateGroupId { get; set; }
        public string MarketTemplateGroupName { get; set; }
        public int SortOrder { get; set; }
        public List<string> MarketTemplateIds { get; set; }
    }
}
