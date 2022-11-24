using System;
using System.Collections.Generic;
using System.Text;
using static Backoffice.Shared.Models.Requests.TopLeaguesRequest;

namespace Backoffice.Shared.Models.Response
{
    public class TopLeaguesResponse
    {
        public TopLeaguesResponse()
        {
            TopLeagues = new List<TopLeague>();
        }
        public List<TopLeague> TopLeagues { get; set; }
    }
}
