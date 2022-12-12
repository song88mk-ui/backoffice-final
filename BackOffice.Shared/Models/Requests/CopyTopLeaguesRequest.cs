using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class CopyTopLeaguesRequest
    {
        public CopyTopLeaguesRequest()
        {
            ClientIdRefs = new List<int>();
            TopLeagues = new List<SportLeague>();
        }

        public List<int> ClientIdRefs { get; set; }
        public int SportId { get; set; }
        public List<SportLeague> TopLeagues { get; set; }

        public class SportLeague 
        {
            public int SportId { get; set; }
            public string SportName { get; set; }
            public bool Status { get; set; }
            public int SortOrder { get; set; }
            public int SegmentId { get; set; }
            public string SegmentName { get; set; }
            public int LeagueId { get; set; }
            public string LeagueName { get; set; }
            public int LiveCount { get; set; }
        }
    }
}
