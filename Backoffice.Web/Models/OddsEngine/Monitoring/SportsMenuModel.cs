using Microsoft.AspNetCore.Components.Web;

namespace Backoffice.Web.Models.OddsEngine.Monitoring
{
    public class SportsMenuModel
    {
        public List<SportItemModel> SportItems { get; set; }
    }
    public class SportItemModel : SportBaseItemModel
    {
        public List<SegmentItemModel> Segments { get; set; }
    }
    public class SegmentItemModel : SportBaseItemModel
    {
        public List<LeagueItemModel> Leagues { get; set; }
    }
    public class LeagueItemModel : SportBaseItemModel
    {
    }
    public class CurrentSportModel
    {
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
    }
    public class SportBaseItemModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
        public int SortOrder { get; set; }
        public bool ShouldRender { get; set; }
        public Action<MouseEventArgs> MenuEvent { get; set; } = e => { };
    }
}
