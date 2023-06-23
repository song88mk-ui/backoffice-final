using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models
{
    public class ConfigTemplateModel
    {
        public long Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; } = "";
        public int SportId { get; set; }
        public int FixtureTypeId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
