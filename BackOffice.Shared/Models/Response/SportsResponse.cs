using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class SportsTreeResponse
    {
        public SportsTreeResponse()
        {
            Sports = new List<SportReponse>();
        }
        public List<SportReponse> Sports { get; set; }

        #region Nested Class

        public class SportReponse
        {
            public SportReponse()
            {
                Segments = new List<SegmentResponse>();
            }

            public int SportId { get; set; }
            public string SportName { get; set; }
            public int? SortOrder { get; set; }
            public List<SegmentResponse> Segments { get; set; }
        }
        public class SegmentResponse
        {
            public SegmentResponse()
            {
                Leagues = new List<LeagueResponse>();
            }
            public int SegmentId { get; set; }
            public string SegmentName { get; set; }
            public int SortOrder { get; set; }
            public List<LeagueResponse> Leagues { get; set; }
        }
        public class LeagueResponse
        {
            public LeagueResponse()
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
