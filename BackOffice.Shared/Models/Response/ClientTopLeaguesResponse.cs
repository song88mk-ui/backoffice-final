using System;
using System.Collections.Generic;
using System.Text;
using static Backoffice.Shared.Models.Requests.TopLeaguesRequest;

namespace Backoffice.Shared.Models.Response
{
    public class ClientTopLeaguesResponse
    {
        public ClientTopLeaguesResponse()
        {
            ClientTopLeagues = new List<OrderedClientTopLeagues>();
        }
        public List<OrderedClientTopLeagues> ClientTopLeagues { get; set; }

        #region Nested Class
        public class OrderedClientTopLeagues
        {
            public int ClientIdRef { get; set; }
            public List<OrderedClientTopLeague> TopLeagues { get; set; }
        }

        public class ClientTopLeaguesRequest
        {
            public int ClientIdRef { get; set; }
            public int SportId { get; set; }
            public List<ClientTopLeague> TopLeagues { get; set; }
        }
        #endregion
    }
}
