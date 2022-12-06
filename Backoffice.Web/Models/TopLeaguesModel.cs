using static Backoffice.Shared.Models.Requests.GetTopLeaguesRequest;

namespace Backoffice.Web.Models
{
    public class TopLeaguesModel
    {
        public TopLeaguesModel()
        {
            OrderedClientTopLeagues = new List<OrderedClientTopLeague>();
        }

        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public List<OrderedClientTopLeague> OrderedClientTopLeagues { get; set; }
    }
}
