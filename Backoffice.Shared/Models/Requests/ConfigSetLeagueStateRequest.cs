using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ConfigSetLeagueStateRequest
    {
        public long PrematchTemplateId { get; set; }
        public long LiveTemplateId { get; set; }
        // BO-151: the state to assign the leagues with. False is Active and is the default on purpose: a caller that does
        // not send it (an older Web build) must never restrict a league by accident.
        public bool Restricted { get; set; }
        public List<ConfigClientModel> Leagues { get; set; }
    }
}
