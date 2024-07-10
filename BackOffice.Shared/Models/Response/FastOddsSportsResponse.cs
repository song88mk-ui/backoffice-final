using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class FastOddsSportsResponse
    {
        public FastOddsSportsResponse()
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
            public int SortOrder { get; set; }
            public List<Segment> Segments { get; set; }
        }

        public class Segment
        {
            public Segment()
            {
                Leagues = new List<League>();
            }

            public int SegmentId { get; set; }
            public string SegmentName { get; set; }
            public int SortOrder { get; set; }
            public List<League> Leagues { get; set; }
        }

        public class League
        {
            public League()
            {
                ProviderFixtureIds = new List<string>();
            }

            public int LeagueId { get; set; }
            public string LeagueName { get; set; }
            public int SortOrder { get; set; }
            public List<string> ProviderFixtureIds { get; set; }
        }

        #endregion
    }
}
