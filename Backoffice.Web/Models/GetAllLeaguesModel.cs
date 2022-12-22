namespace Backoffice.Web.Models
{
    public class GetAllLeaguesModel
    {
        public GetAllLeaguesModel()
        {
            TopLeagues = new List<Leagues>();
        }
        public List<Leagues> TopLeagues { get; set; }

        public class Leagues
        {
            public int SegmentId { get; set; }
            public string SegmentName { get; set; }
            public int LeagueId { get; set; }
            public string LeagueName { get; set; }
            public int LiveCount { get; set; }
        }
    }
}
