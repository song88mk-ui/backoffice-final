using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    // BO-122: one League Sorting import file for one client and one sport.
    public class ImportClientOddstableLeagueSettingRequest
    {
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        // false = validate only: nothing is saved, audited or published.
        public bool Commit { get; set; }
        public List<ImportOddstableLeagueRow> Rows { get; set; } = new List<ImportOddstableLeagueRow>();
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    // One data row of the import file. Names in the file are informational, so only the ids travel.
    public class ImportOddstableLeagueRow
    {
        // Row number as a spreadsheet shows it (the header is row 1), so messages point at the right row.
        public int RowNo { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public int SortOrder { get; set; }
    }
}
