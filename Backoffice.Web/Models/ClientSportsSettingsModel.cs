using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class ClientSportsSettingsModel
    {
        public ClientSportsSettingsModel()
        {
            Sports = new List<SportSettingSport>();
        }

        public List<SportSettingSport> Sports { get; set; }
    }
    public class SportSettingSport
    {
        public SportSettingSport()
        {
            Segments = new List<SportSettingSegment>();
        }

        public int SportId { get; set; }

        public string SportName { get; set; }

        public int SortOrder { get; set; }

        public List<SportSettingSegment> Segments { get; set; }
        public bool IsActive { get; set; }
        public bool IsOpen { get; set; }

    }

    public class SportSettingSegment
    {
        public SportSettingSegment()
        {
            Leagues = new List<SportSettingLeague>();
        }

        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int SortOrder { get; set; }
        public List<SportSettingLeague> Leagues { get; set; }
        public bool IsActive { get; set; }
        public bool IsOpen { get; set; }
    }

    public class SportSettingLeague
    {
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
