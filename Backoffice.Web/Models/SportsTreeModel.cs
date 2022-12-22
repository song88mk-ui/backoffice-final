using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class SportsTreeModel
    {
        public SportsTreeModel()
        {
            Sports = new List<SportModel>();
        }
        public List<SportModel> Sports { get; set; }

        #region Nested Class

        public class SportModel
        {
            public SportModel()
            {
                Segments = new List<SegmentModel>();
            }

            public int SportId { get; set; }
            public string SportName { get; set; }
            public int? SortOrder { get; set; }
            public List<SegmentModel> Segments { get; set; }
        }
        public class SegmentModel
        {
            public SegmentModel()
            {
                Leagues = new List<LeagueModel>();
            }
            public int SegmentId { get; set; }
            public string SegmentName { get; set; }
            public int SortOrder { get; set; }
            public List<LeagueModel> Leagues { get; set; }
        }
        public class LeagueModel
        {
            public LeagueModel()
            {
                Fixtures = new List<FixtureModel>();
            }
            public int LeagueId { get; set; }
            public string LeagueName { get; set; }
            public int SortOrder { get; set; }
            public List<FixtureModel> Fixtures { get; set; }
        }

        #endregion
    }
}
