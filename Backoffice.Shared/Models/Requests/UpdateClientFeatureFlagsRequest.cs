using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class UpdateClientFeatureFlagsRequest
    {
        public string ClientCode { get; set; }
        public bool BettingRules { get; set; }
        public bool DarkMode { get; set; }
        public bool LanguageOptions { get; set; }
        public bool OGAuth { get; set; }
        public bool FullCashout { get; set; }
        public bool PartialCashout { get; set; }
        public bool BetRecommendation { get; set; }
        public bool IsEuroLayout { get; set; }

        // Null means "leave unchanged" for both fields below: the repository only merges
        // them into the stored webprops document when a caller explicitly supplies them,
        // so older callers that omit them can never wipe the stored values.
        public List<string> GameTrackerProductNames { get; set; }
        public Dictionary<string, List<string>> LayoutThemes { get; set; }
    }
}
