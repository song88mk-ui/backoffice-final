using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class ConfigClientModel
    {
        public bool IsChecked { get; set; } = false;
        public long Id { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long TemplateId { get; set; } = -1;
        public string TemplateName { get; set; } = "";
    }
}
