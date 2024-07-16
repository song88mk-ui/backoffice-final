using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ConfigSetLeagueStateRequest
    {
        public long PrematchTemplateId { get; set; }
        public long LiveTemplateId { get; set; }
        public List<ConfigClientModel> Leagues { get; set; }
    }
}
