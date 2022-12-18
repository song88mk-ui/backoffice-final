using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models
{
    public class EditTopLeaguesModel
    {
        public EditTopLeaguesModel()
        {
            Sports = new List<SportsTreeResponse.SportReponse>();
        }

        public List<SportsTreeResponse.SportReponse> Sports { get; set; }
        public SportsTreeResponse.SportReponse SelectedSport { get; set; }
        public SportsTreeResponse.SegmentResponse SelectedSegment { get; set; }
        public SportsTreeResponse.LeagueResponse SelectedLeague { get; set; }
    }
}
