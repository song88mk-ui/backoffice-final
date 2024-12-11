using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models
{
    public class SportClickCallbackModel
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long UnixTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
