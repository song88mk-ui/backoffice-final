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
        // BO-151: the saved state of an assigned league for the client. False is Active (reviewed, visible), true is
        // Restricted (hidden for that client). It means nothing for an unassigned league.
        public bool IsRestricted { get; set; }
    }
}
