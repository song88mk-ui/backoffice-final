using Backoffice.Shared.Models;
using Microsoft.AspNetCore.Components.Web;

namespace Backoffice.Web.Models.OddsEngine.Monitoring
{
    public class MonitoringFixtureModel
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
        public bool IsLiveStream { get; set; }
        public bool IsSuspended { get; set; }
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
        public string Score { get; set; }
        public string CurrentPeriod { get; set; }
        public string CurrentMinute { get; set; }
        public List<int> BetStop { get; set; }
        public bool IsBetStopGlobal { get; set; }
        public int SortOrder { get; set; }
        public int FixtureTypeId { get; set; }
        public long UnixTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public List<MonitoringFixtureClient> ClientMarkets { get; set; } = new List<MonitoringFixtureClient>();
    }
    public class MonitoringFixtureClient
    {
        public string ClientCode { get; set; }
        public long UnixTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public MonitoringFixtureMarketModel? Market { get; set; }
    }
    public class MonitoringFixtureMarketModel
    {
        public string ClientCode { get; set; }
        public decimal ClientCodeMargin { get; set; }
        public string ProviderFixtureId { get; set; }
        public string MarketTemplateId { get; set; }
        public string MarketId { get; set; }
        public string ProviderMarketId { get; set; }
        public string MarketName { get; set; }
        public string Line { get; set; }
        public int MarketStatusId { get; set; }
        public string MarketStatusName { get; set; }
        public string MarketSpecifier { get; set; }
        public bool IsBoosted { get; set; }
        public long UnixTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public List<MonitoringFixtureSelectionModel> Selections { get; set; } = new List<MonitoringFixtureSelectionModel>();
        public bool IsBetStop { get; set; }
    }
    public class MonitoringFixtureSelectionModel
    {
        public string ProviderSelectionId { get; set; }
        public string SelectionId { get; set; }
        public string SelectionTemplateId { get; set; }
        public string SelectionName { get; set; }
        public string SelectionSpecifier { get; set; }
        public string DecimalOdds { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public long UnixTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
