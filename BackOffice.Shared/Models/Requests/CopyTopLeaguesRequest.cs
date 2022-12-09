using System.Collections.Generic;
using static Backoffice.Shared.Models.Requests.GetTopLeaguesRequest;

namespace Backoffice.Shared.Models.Requests
{
    public class CopyTopLeaguesRequest
    {
        public CopyTopLeaguesRequest()
        {
            ClientIdRefs = new List<int>();
            TopLeagues = new List<OrderedClientTopLeague>();
        }

        public List<int> ClientIdRefs { get; set; }
        public int SportId { get; set; }
        public List<OrderedClientTopLeague> TopLeagues { get; set; }

        public class OrderedClientTopLeague
        {
            public int SportId { get; set; }
            public string SportName { get; set; }
            public int SegmentId { get; set; }
            public string SegmentName { get; set; }
            public int LeagueId { get; set; }
            public string LeagueName { get; set; }
            public int SortOrder { get; set; }
        }
    }
}
