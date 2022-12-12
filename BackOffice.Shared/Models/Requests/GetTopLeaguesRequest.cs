namespace Backoffice.Shared.Models.Requests
{
    public class GetTopLeaguesRequest
    {
        public string ClientIdRefs { get; set; }
        public int SportId { get; set; }
        public string LanguageCode { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }

        public class ClientTopLeague 
        {
            public int SportId { get; set; }
            public string SportName { get; set; }
            public int SegmentId { get; set; }
            public int LeagueId { get; set; }
            public string SegmentName { get; set; }
            public string LeagueName { get; set; }

        }

        public class OrderedClientTopLeague
        {
            public int SportId { get; set; }
            public string SportName { get; set; }
            public int SegmentId { get; set; }
            public int LeagueId { get; set; }
            public string SegmentName { get; set; }
            public string LeagueName { get; set; }
            public bool Status { get; set; }
            public int SortOrder { get; set; }
            public int LiveCount { get; set; }
        }

    }
}
