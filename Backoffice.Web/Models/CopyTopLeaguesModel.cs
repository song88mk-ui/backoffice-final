namespace Backoffice.Web.Models
{
    public class CopyTopLeaguesModel
    {
        public CopyTopLeaguesModel()
        {
            ClientIdSelectedModel = new List<ClientIdSelectedModel>();
            CopyFromClientTopLeagues = new List<TopLeague>();
        }

        public int CopyFromClientId { get; set; }
        public int SportId { get; set; }
        public List<ClientIdSelectedModel> ClientIdSelectedModel { get; set; }
        public List<TopLeague> CopyFromClientTopLeagues { get; set; }


        #region Nested Class

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
