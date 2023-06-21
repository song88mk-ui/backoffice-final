using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Backoffice.Shared.Data.Backoffice.Entities
{
    public class ConfigTemplateMargin
    {
        [Key]
        public long ConfigTemplateId { get; set; }
        [Key]
        public int SportId { get; set; }
        [Key]
        public int FixtureTypeId { get; set; }
        [Key]
        public string MarketTemplateId { get; set; }
        public bool IsActive { get; set; }
        public string Prematch { get; set; }
        public string Live { get; set; }
    }
}
