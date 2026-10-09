using System;

namespace Backoffice.Shared.Models.Response
{
    public class ClientOddstableLeagueOrderListResponse
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int SortOrder { get; set; }
    }
}
