namespace Backoffice.Shared.Models.Requests
{
    public class GetAllLeaguesRequest
    {
        public int ClientIdRef { get; set; }
        public int SportId { get; set; }
        public string LanguageCode { get; set; }
        public int LeagueId { get; set; }
        public int SegmentId { get; set; }
    }
}
