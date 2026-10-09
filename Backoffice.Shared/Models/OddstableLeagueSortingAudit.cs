using System.Collections.Generic;

namespace Backoffice.Shared.Models
{
    // The From and To of a League Sorting entry in ActivityLog2 (Page = SBC.OE.LeagueSorting, Payload = the
    // client code). Written by SportsbookRepository, read back by the League Sorting logs dialog.
    public class OddstableLeagueSortingAudit
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        // Only set on the To side of a Copy entry: the client the order was copied from.
        public string SourceClientCode { get; set; }
        public List<OddstableLeagueSortingAuditLeague> Leagues { get; set; } = new List<OddstableLeagueSortingAuditLeague>();
    }

    public class OddstableLeagueSortingAuditLeague
    {
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int SortOrder { get; set; }
    }
}
