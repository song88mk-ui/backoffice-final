using static Backoffice.Shared.Models.Requests.GetTopLeaguesRequest;

namespace Backoffice.Web.Models
{
    public class TopLeaguesModel
    {
        public TopLeaguesModel()
        {
            ClientTopLeagues = new List<ClientTopLeague>();
        }

        public List<ClientTopLeague> ClientTopLeagues { get; set; }

        #region Nested Class

        public class ClientTopLeague
        {
            public ClientTopLeague()
            {
                TopLeagues = new List<TopLeague>();
            }

            public int RowNo { get; set; }
            public int ClientIdRef { get; set; }
            public string ClientName { get; set; }
            public List<TopLeague> TopLeagues { get; set; }
        }

        public class TopLeague
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

        #endregion
    }
}
