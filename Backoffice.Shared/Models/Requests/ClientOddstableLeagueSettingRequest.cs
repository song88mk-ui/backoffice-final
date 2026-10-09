using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class ClientOddstableLeagueSettingRequest
    {
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public List<OddstableLeagueSortOrder> Leagues { get; set; } = new List<OddstableLeagueSortOrder>();
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    public class OddstableLeagueSortOrder
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int SortOrder { get; set; }
    }
}
