using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class UpdateClientFeatureFlagsRequest
    {
        // BO-108: despite the historical name, this is the client's Platform GUID
        // (PlatformClientResponse.ClientId), not an SBC client code.
        public string ClientId { get; set; }
        public bool BettingRules { get; set; }
        public bool DarkMode { get; set; }
        public bool LanguageOptions { get; set; }
        public bool OGAuth { get; set; }

        // Null means "leave unchanged" for FullCashout (BO-108) and for the two fields
        // below it: the repository only merges them into the stored webprops document
        // when a caller explicitly supplies them, so a caller that omits them can never
        // wipe the stored value.
        public bool? FullCashout { get; set; }
        public bool PartialCashout { get; set; }
        public bool BetRecommendation { get; set; }
        public bool IsEuroLayout { get; set; }

        public List<string> GameTrackerProductNames { get; set; }
        public Dictionary<string, List<string>> LayoutThemes { get; set; }
    }
}
