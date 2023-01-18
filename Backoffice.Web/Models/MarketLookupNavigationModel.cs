using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models
{
    public class MarketLookupNavigationModel
    {
        public bool IsSelected { get; set; }
        public int MarketTemplateGroupId { get; set; }
        public MarketLookupModel MarketLookup { get; set; }
        public MarketLookupModel NewMarketLookup { get; set; }
        public PrematchSettingsModel PrematchSettings { get; set; }
        public LiveSettingsModel LiveSettings { get; set; }
        public PrematchSettingsModel NewPrematchSettings { get; set; }
        public LiveSettingsModel NewLiveSettings { get; set; }
    }
}
