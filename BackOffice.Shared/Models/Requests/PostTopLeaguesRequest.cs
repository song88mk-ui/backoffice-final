using System.Collections.Generic;
using static Backoffice.Shared.Models.Requests.GetTopLeaguesRequest;

namespace Backoffice.Shared.Models.Requests
{
    public class PostTopLeaguesRequest
    {
        public int ClientIdRef { get; set; }
        public int SportId { get; set; }
        public List<ClientTopLeague> TopLeagues { get; set; }
    }
}
