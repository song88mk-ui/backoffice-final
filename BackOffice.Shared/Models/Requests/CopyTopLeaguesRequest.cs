using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class CopyTopLeaguesRequest
    {
        public CopyTopLeaguesRequest()
        {
            ClientIds = new List<int>();
            TopLeagues = new List<ClientTopSetup>();
        }

        public List<int> ClientIds { get; set; }
        public int SportId { get; set; }
        public List<ClientTopSetup> TopLeagues { get; set; }
    }
}
