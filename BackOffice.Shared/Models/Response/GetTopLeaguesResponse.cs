using System.Collections.Generic;
using static Backoffice.Shared.Models.Requests.GetTopLeaguesRequest;

namespace Backoffice.Shared.Models.Response
{
    public class GetTopLeaguesResponse
    {
        public GetTopLeaguesResponse()
        {
            ClientTopLeagues = new List<OrderedClientTopLeagues>();
        }
        public List<OrderedClientTopLeagues> ClientTopLeagues { get; set; }

        #region Nested Class
        public class OrderedClientTopLeagues
        {
            public int ClientIdRef { get; set; }
            public string ClientName { get; set; }
            public int SportId { get; set; }
            public List<OrderedClientTopLeague> TopLeagues { get; set; }
        }
        #endregion
    }
}
