using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.HubMessages
{
    public class OddsChangeFixtureDetailsMessage
    {
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
        public bool IsSuspended { get; set; }
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
        public string Score { get; set; }
        public string CurrentPeriod { get; set; }
        public int CurrentMinute { get; set; }
        public int SortOrder { get; set; }
        public int FixtureTypeId { get; set; }
        public List<int> BetStop {  get; set; }
        public bool IsBetStopGlobal { get; set; }
        public long UnixTime { get; set; }
        public List<OddsChangeFixtureDetailsMarket> Markets { get; set; } = new List<OddsChangeFixtureDetailsMarket>();
    }
    public class OddsChangeFixtureDetailsMarket
    {
        public string MarketTemplateId { get; set; }
        public string MarketId { get; set; }
        public string ProviderMarketId { get; set; }
        public string MarketName { get; set; }
        public string Line { get; set; }
        public int MarketStatusId { get; set; }
        public string MarketStatusName { get; set; }
        public string MarketSpecifier { get; set; }
        public string ClientCode { get; set; } // To Be Remove
        public string ClientCodeMargin { get; set; } // To Be Remove
        public Dictionary<string, decimal> ClientCodeMargins { get; set; }
        public int SortOrder { get; set; }
        public long UnixTime { get; set; }
        public List<OddsChangeFixtureDetailsClientSelections> ClientSelections { get; set; } = new List<OddsChangeFixtureDetailsClientSelections>();
        public List<OddsChangeFixtureDetailsSelection> Selections { get; set; } = new List<OddsChangeFixtureDetailsSelection>();
    }
    public class OddsChangeFixtureDetailsClientSelections
    {
        public string ClientCode { get; set; }
        public string ClientMargin { get; set; }

        public List<OddsChangeFixtureDetailsSelection> Selections { get; set; } = new List<OddsChangeFixtureDetailsSelection>();
    }
    public class OddsChangeFixtureDetailsSelection
    {
        public string ProviderSelectionId { get; set; }
        public long SelectionId { get; set; }
        public string SelectionTemplateId { get; set; }
        public string SelectionName { get; set; }
        public decimal DecimalOdds { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public long UnixTime { get; set; }
    }
    public class OddsChangeFixtureDetailsClientOdds
    {
        public string ClientCode { get; set; }
        public string ClientMargin { get; set; }
        public decimal Odds { get; set; }
        public string SelectionTemplateId { get; set; }
        public string SelectionSpecifier { get; set; }
        public string SelectionName { get; set; }
        public long SelectionId { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }
}
