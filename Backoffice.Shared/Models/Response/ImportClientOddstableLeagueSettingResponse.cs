using Backoffice.Shared.Enums;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class ImportClientOddstableLeagueSettingResponse
    {
        public StatusCode StatusCode { get; set; }
        public string Message { get; set; }
        // true only when the new order was saved.
        public bool Committed { get; set; }
        // The imported order differs from the current one.
        public bool HasChanges { get; set; }
        // Rows of the file that matched a league of the sport.
        public int MatchedCount { get; set; }
        // Rows of the file that did not, and were skipped.
        public int UnmatchedCount { get; set; }
        // Leagues of the client that are not in the file and keep their place after the imported ones.
        public int RetainedCount { get; set; }
        // Leagues the client has after the import.
        public int ResultCount { get; set; }
        // Problems that stop the whole import. When there are any, nothing is applied.
        public List<string> Errors { get; set; } = new List<string>();
        public List<ImportOddstableLeagueRowResult> Rows { get; set; } = new List<ImportOddstableLeagueRowResult>();
    }

    public class ImportOddstableLeagueRowResult
    {
        public int RowNo { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int SortOrder { get; set; }
        public bool Matched { get; set; }
        // Why the row was skipped. Empty when it matched.
        public string Reason { get; set; }
    }
}
