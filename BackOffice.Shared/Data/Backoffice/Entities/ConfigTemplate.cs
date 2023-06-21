using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Backoffice.Shared.Data.Backoffice.Entities
{
    public class ConfigTemplate
    {
        [Key]
        public long Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public int SportId { get; set; }
        public int FixtureTypeId { get; set; }        
        public DateTime Timestamp { get; set; }
    }
}
