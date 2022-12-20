using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class TierLeagueOrderResponse
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public string SegmentName { get; set; }
        public string LeagueName { get; set; }
        public int Tier { get; set; }
        public int SortOrder { get; set; }
    }
}
