namespace Backoffice.Web.Models.OddsEngine.LeagueSorting
{
    public class LeagueSortingLeague
    {
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int SortOrder { get; set; }
    }

    public class LeagueSortingClient
    {
        public int RowNo { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public List<LeagueSortingLeague> Leagues { get; set; } = new List<LeagueSortingLeague>();
    }

    // One entry of the logs dialog, already shaped for display.
    public class LeagueSortingLogRow
    {
        public DateTime Timestamp { get; set; }
        public string ClientName { get; set; }
        public string Action { get; set; }
        public string SportName { get; set; }
        public string CopiedFromClientName { get; set; }
        public List<string> PreviousLeagues { get; set; } = new List<string>();
        public List<string> NewLeagues { get; set; } = new List<string>();
        public string UpdatedBy { get; set; }
    }

    // One row of the edit dialog. The key identifies the row across drags, since a row without a
    // league yet has no league id to identify it by.
    public class LeagueSortingEditRow
    {
        public Guid Key { get; set; } = Guid.NewGuid();
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }

        public bool IsEmpty => LeagueId == 0;
    }
}
