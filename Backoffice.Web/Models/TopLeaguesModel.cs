using static Backoffice.Shared.Models.Requests.GetTopLeaguesRequest;

namespace Backoffice.Web.Models
{
    public class TopLeaguesModel
    {
        public TopLeaguesModel()
        {
            ClientTopLeagues = new List<OrderedClientTopLeagues>();
        }
        public List<OrderedClientTopLeagues> ClientTopLeagues { get; set; }

        public class OrderedClientTopLeagues
        {
            public int ClientIdRef { get; set; }
            public string ClientName { get; set; }
            public List<OrderedClientTopLeague> TopLeagues { get; set; }
        }
    }
}
