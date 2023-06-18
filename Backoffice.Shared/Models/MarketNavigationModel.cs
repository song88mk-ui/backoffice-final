
using Backoffice.Shared.Models.Response;

namespace Backoffice.Shared.Models
{
    public class MarketNavigationModel
    {
        public string Id { get; set; }
        public bool IsSelected { get; set; }
        public string MarketTemplateId { get; set; }
        public string MarketName { get; set; }
        public int MarketTemplateGroupId { get; set; }
        public MarketFixtureResponse.FixtureModel Fixture { get; set; }
    }
}
