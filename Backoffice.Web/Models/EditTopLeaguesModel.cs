namespace Backoffice.Web.Models
{
    public class EditTopLeaguesModel
    {
        public EditTopLeaguesModel()
        {
            Sports = new List<SportModel>();
        }

        public List<SportModel> Sports { get; set; }
        public SportModel SelectedSport { get; set; }
        public SegmentModel SelectedSegment { get; set; }
        public LeagueModel SelectedLeague { get; set; }
    }
}
