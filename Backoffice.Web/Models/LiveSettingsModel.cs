using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class LiveSettingsModel
    {
        public string OvertimeKey { get; set; }
        public string FirstKey { get; set; }
        public string LastKey { get; set; }
        public string LastTimeKey { get; set; }
        public List<KeyTimeModel> Settings { get; set; }

        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public string MarketTemplateId { get; set; }
    }
}
