using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Backoffice.Shared.Data.Backoffice.Entities
{
    public class ConfigTemplateAssigned
    {
        [Key]
        public long Id { get; set; }
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public long ConfigTemplateId { get; set; }
    }
}
