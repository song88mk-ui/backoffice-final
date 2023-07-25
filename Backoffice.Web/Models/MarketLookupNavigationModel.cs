using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models
{
    public class MarketLookupNavigationModel
    {
        public bool IsSelected { get; set; }
        public List<int> MarketTemplateGroupIds { get; set; }
        public MarketLookupModel MarketLookup { get; set; }
        public MarketLookupModel NewMarketLookup { get; set; }
        public PrematchSettingsTemplateModel PrematchSettings { get; set; }
        public LiveSettingsTemplateModel LiveSettings { get; set; }
        public PrematchSettingsTemplateModel NewPrematchSettings { get; set; }
        public LiveSettingsTemplateModel NewLiveSettings { get; set; }
    }
}
