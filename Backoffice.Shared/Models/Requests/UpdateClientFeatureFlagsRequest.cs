namespace Backoffice.Shared.Models.Requests
{
    public class UpdateClientFeatureFlagsRequest
    {
        public string ClientCode { get; set; }
        public bool BettingRules { get; set; }
        public bool DarkMode { get; set; }
        public bool LanguageOptions { get; set; }
        public bool OGAuth { get; set; }
        public bool PartialCashout { get; set; }
        public bool BetRecommendation { get; set; }
    }
}
