using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class PlatformGetTopLeaguesResponse
    {
        public PlatformGetTopLeaguesResponse()
        {
            TopLeagues = new List<ClientTopLeague>();
        }

        public List<ClientTopLeague> TopLeagues { get; set; }
        
        public class ClientTopLeague
        {
            public int ClientIdRef { get; set; }
            public int SportId { get; set; }
            public List<SportLeague> TopLeagues { get; set; }
        }

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
