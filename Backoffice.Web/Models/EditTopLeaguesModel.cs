namespace Backoffice.Web.Models
{
    public class EditTopLeaguesModel
    {
        public EditTopLeaguesModel()
        {
            Sports = new List<SportsTreeModel.SportModel>();
        }

        public List<SportsTreeModel.SportModel> Sports { get; set; }
        public SportsTreeModel.SportModel SelectedSport { get; set; }
        public SportsTreeModel.SegmentModel SelectedSegment { get; set; }
        public SportsTreeModel.LeagueModel SelectedLeague { get; set; }
    }
}
