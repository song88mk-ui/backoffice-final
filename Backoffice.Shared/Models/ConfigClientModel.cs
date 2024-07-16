using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class ConfigClientModel
    {
        public long Id { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long PrematchTemplateId { get; set; } = -1;
        public long LiveTemplateId { get; set; } = -1;
        public string PrematchTemplateName { get; set; } = "";
        public string LiveTemplateName { get; set; } = "";
    }
}
