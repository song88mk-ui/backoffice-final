using System.Collections.Generic;
using System.Security.Policy;
using System.Text.Json.Serialization;

namespace Backoffice.Shared.ExternalApis.Maintenance
{
    public class ExternalMaintenanceResponse
    {
        [JsonPropertyName("Config")]
        public List<ExternalMaintenanceConfig> Configs { get; set; }
    }
    public class ExternalMaintenanceConfig
    {
        public string ClientId { get; set; }
        public bool IsMaintenance { get; set; }
        public string Site { get; set; }
    }
}
