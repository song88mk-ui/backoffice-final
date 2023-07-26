using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class ConfigTemplateLeagueResponse
    {
        public List<ConfigClientModel> ClientConfigs { get; set; }
        public MetaData MetaData { get; set; }
    }
}
