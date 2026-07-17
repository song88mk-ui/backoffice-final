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

        // webprops-api has no fixed schema (stores config as a generic JSON map), so this
        // round-trips through S3 fine. It has no downstream effect until platform-web's own
        // Features model and Index.razor are updated to read and gate on it.
        public bool BetRecommendation { get; set; }
    }

    public class ClientLayoutTheme
    {
        public List<string> Themes { get; set; } = new List<string>();
    }
}
