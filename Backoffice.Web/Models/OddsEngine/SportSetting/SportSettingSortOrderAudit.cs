namespace Backoffice.Web.Models.OddsEngine.SportSetting
{
    public class SportSettingSortOrderAudit
    {
        public List<SportSortOrderAudit> Sports { get; set; }
        public List<SegmentSortOrderAuditSport> Segments { get; set; }
        public List<LeagueSortOrderAuditSport> Leagues { get; set; }
    }
    public class SportSortOrderAudit
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SortOrder { get; set; }
    }

    public class SegmentSortOrderAuditSport
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public List<SegmentSortOrderAuditSegment> Segments { get; set; }
    }
    public class SegmentSortOrderAuditSegment
    {
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int SortOrder { get; set; }
    }


    public class LeagueSortOrderAuditSport
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public List<LeagueSortOrderAuditSegment> Segments { get; set; }
    }
    public class LeagueSortOrderAuditSegment
    {
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public List<LeagueSortOrderAuditLeague> Leagues { get; set; }
    }
    public class LeagueSortOrderAuditLeague
    {
        public long LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int SortOrder { get; set; }
    }
}
