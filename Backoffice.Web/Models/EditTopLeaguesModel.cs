using static Backoffice.Shared.Models.Requests.GetTopLeaguesRequest;

namespace Backoffice.Web.Models
{
    public class EditTopLeaguesModel
    {
        public int ClientId { get; set; }
        public string SportId { get; set; }
        public List<OrderedClientTopLeague> CurrentTopLeagues { get; set; }
    }
}
