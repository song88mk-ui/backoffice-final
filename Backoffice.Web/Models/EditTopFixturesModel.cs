using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models
{
    public class EditTopFixturesModel
    {
        public EditTopFixturesModel()
        {
            Sports = new List<SportsTreeResponse.SportReponse>();
        }
        public List<SportsTreeResponse.SportReponse> Sports { get; set; }
        public SportsTreeResponse.SportReponse SelectedSport { get; set; }
        public SportsTreeResponse.SegmentResponse SelectedSegment { get; set; }
        public SportsTreeResponse.LeagueResponse SelectedLeague { get; set; }
        public FixtureModel SelectedGameFilter { get; set; }
        public FixtureModel SelectedGame { get; set; }
        public string SortFilter { get; set; }

    }
}
