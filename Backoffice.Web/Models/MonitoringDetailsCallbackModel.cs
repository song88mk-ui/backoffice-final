using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models
{
    public class MonitoringDetailsCallbackModel
    {
        public string SelectedMarketTemplateId { get; set; }
        public MarketFixtureDetailResponse Fixture { get; set; }
    }
}
