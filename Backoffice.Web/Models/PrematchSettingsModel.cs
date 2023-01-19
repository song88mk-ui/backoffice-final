namespace Backoffice.Web.Models
{
    public class PrematchSettingsModel
    {
        public string FirstKey { get; set; }
        public string LastKey { get; set; }
        public List<Backoffice.Shared.Models.KeyTimeModel> Settings { get; set; }

        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public string MarketTemplateId { get; set; }
    }
}
