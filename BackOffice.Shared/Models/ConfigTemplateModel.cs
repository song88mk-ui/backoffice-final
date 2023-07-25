using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models
{
    public class ConfigTemplateModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public int SportId { get; set; }
        public int FixtureTypeId { get; set; }
        public int ClientsCount { get; set; } = 0;
        public int LeaguesCount { get; set; } = 0;
        public DateTime Timestamp { get; set; }
    }
}
