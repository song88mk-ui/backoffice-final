using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class LeagueLookupResponse
    {
        public LeagueLookupResponse()
        {
            LeagueLookup = new List<LeagueLookupModel>();
        }

        public List<LeagueLookupModel> LeagueLookup { get; set; }

        #region Nested Class
        public class LeagueLookupModel
        {
            public int LeagueId { get; set; }
            public string LeagueName { get; set; }
            public int SportId { get; set; }
            public int SegmentId { get; set; }
        }
        #endregion
    }
}
