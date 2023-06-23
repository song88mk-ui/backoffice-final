using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ConfigTemplateLeagueRequest
    {
        public ConfigTemplateLeagueRequest()
        {
            MetaData = new MetaData()
            {
                CurrentPage = 0,
                PageSize = -1
            };
        }
        public string SearchText { get; set; } = string.Empty;
        public List<string> ClientCodes { get; set; } = new List<string>();
        public List<int> SportIds { get; set; } = new List<int>();
        public List<int> ScopeIds { get; set; } = new List<int>();
        public MetaData MetaData { get; set; } = new MetaData();
    }
}
