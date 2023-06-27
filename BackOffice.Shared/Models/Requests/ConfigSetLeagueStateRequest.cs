using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ConfigSetLeagueStateRequest
    {
        public long TemplateId { get; set; }
        public List<ConfigClientModel> Leagues { get; set; }
    }
}
