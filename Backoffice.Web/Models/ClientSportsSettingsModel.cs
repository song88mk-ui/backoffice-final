namespace Backoffice.Web.Models
{
    public class ClientSportsSettingsModel
    {
        public ClientSportsSettingsModel()
        {
            Sports = new List<Sport>();
        }

        public List<Sport> Sports { get; set; }

        #region Nested Class

        public class Sport
        {
            public Sport()
            {
                Segments = new List<Segment>();
            }

            public int SportId { get; set; }
            public string SportName { get; set; }
            public List<Segment> Segments { get; set; }
            public int SortOrder { get; set; }
            public bool IsActive { get; set; }
            public bool IsOpen { get; set; }

        }

        public class Segment
        {
            public Segment()
            {
                Leagues = new List<League>();
            }

            public int SegmentId { get; set; }
            public string SegmentName { get; set; }
            public List<League> Leagues { get; set; }
            public int SortOrder { get; set; }
            public bool IsActive { get; set; }
            public bool IsOpen { get; set; }
        }

        public class League
        {
            public int LeagueId { get; set; }
            public string LeagueName { get; set; }
            public int SortOrder { get; set; }
            public bool IsActive { get; set; }
        }

        #endregion
    }
}
