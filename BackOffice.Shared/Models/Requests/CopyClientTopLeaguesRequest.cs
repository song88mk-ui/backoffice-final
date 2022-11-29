using System.Collections.Generic;
using static Backoffice.Shared.Models.Requests.TopLeaguesRequest;

namespace Backoffice.Shared.Models.Requests
{
    public class CopyClientTopLeaguesRequest
    {
        public CopyClientTopLeaguesRequest()
        {
            ClientIds = new List<int>();
            TopLeagues = new List<ClientTopLeague>();
        }

        public List<int> ClientIds { get; set; }
        public int SportId { get; set; }
        public List<ClientTopLeague> TopLeagues { get; set; }
    }
}
