using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class ConfigClientModel
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public int LeagueId { get; set; }
        public string TemplateId { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
    }
}
