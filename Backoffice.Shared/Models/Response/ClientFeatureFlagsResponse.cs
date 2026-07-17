using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class ClientFeatureFlagsResponse
    {
        public string ClientCode { get; set; }
        public ClientFeatureFlags Features { get; set; } = new ClientFeatureFlags();
        public Dictionary<string, ClientLayoutTheme> Layout { get; set; } = new Dictionary<string, ClientLayoutTheme>();
    }

    public class ClientFeatureFlags
    {
        public bool BettingRules { get; set; }
        public bool DarkMode { get; set; }
        public List<string> GameTrackerProductNames { get; set; } = new List<string>();
        public bool LanguageOptions { get; set; }
        public bool OGAuth { get; set; }
        public bool PartialCashout { get; set; }

        // Backoffice-only addition: not yet in webprops-api's real schema and not read by
        // platform-web. Stored/round-tripped like the other flags, but has no downstream
        // effect until platform-web is updated to gate BetRecommendation on it.
        public bool BetRecommendation { get; set; }
    }

    public class ClientLayoutTheme
    {
        public List<string> Themes { get; set; } = new List<string>();
    }
}
