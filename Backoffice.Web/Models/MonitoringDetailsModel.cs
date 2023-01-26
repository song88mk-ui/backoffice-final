using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class MonitoringDetailsModel
    {
        public MonitoringDetailsModel()
        {
            Fixtures = new List<FixtureMarketModel>();
        }

        public string SelectedMarketTemplateId { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public string SegmentName { get; set; }
        public string LeagueName { get; set; }
        public List<FixtureMarketModel> Fixtures { get; set; }
    }
}
