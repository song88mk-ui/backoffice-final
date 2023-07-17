using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class EditTopFixturesModel
    {
        public EditTopFixturesModel()
        {
            Sports = new List<SportModel>();
        }
        public List<SportModel> Sports { get; set; }
        public SportModel SelectedSport { get; set; }
        public SegmentModel SelectedSegment { get; set; }
        public LeagueModel SelectedLeague { get; set; }
        public FixtureModel SelectedGameFilter { get; set; }
        public FixtureModel SelectedGame { get; set; }
        public string SortFilter { get; set; }

    }
}
