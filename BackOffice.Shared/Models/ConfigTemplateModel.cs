using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models
{
    public class ConfigTemplateModel
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public List<string> ClientCodes { get; set; }
        public List<int> LeagueIds { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
