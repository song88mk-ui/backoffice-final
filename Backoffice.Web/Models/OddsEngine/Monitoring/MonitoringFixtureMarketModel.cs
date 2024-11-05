using Microsoft.AspNetCore.Components.Web;

namespace Backoffice.Web.Models.OddsEngine.Monitoring
{
    public class MonitoringFixtureMarketModel
    {
        public string StartDate { get; set; }
        public string FixtureName { get; set; }
        public Action<MouseEventArgs> CloseMatchButton { get; set; }
        public List<MonitoringFixtureSelectionModel> Selections {  get; set; }
    }
    public class MonitoringFixtureClient
    {
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public decimal Odds { get; set; }
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
        public string CorrelationId { get; set; }
        public List<MonitoringFixtureClient> ClientOdds { get; set; }
    }
}
