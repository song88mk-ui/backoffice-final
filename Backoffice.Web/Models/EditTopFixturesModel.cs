using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class EditTopFixturesModel
    {
        public EditTopFixturesModel()
        {
            Sports = new List<SportsTreeModel.SportModel>();
        }
        public List<SportsTreeModel.SportModel> Sports { get; set; }
        public SportsTreeModel.SportModel SelectedSport { get; set; }
        public SportsTreeModel.SegmentModel SelectedSegment { get; set; }
        public SportsTreeModel.LeagueModel SelectedLeague { get; set; }
        public FixtureModel SelectedGameFilter { get; set; }
        public FixtureModel SelectedGame { get; set; }
        public string SortFilter { get; set; }

    }
}
