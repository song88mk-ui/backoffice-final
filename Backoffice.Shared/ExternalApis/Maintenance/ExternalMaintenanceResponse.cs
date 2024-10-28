using System.Collections.Generic;
using System.Security.Policy;
using System.Text.Json.Serialization;

namespace Backoffice.Shared.ExternalApis.Maintenance
{
    public class ExternalMaintenanceResponse
    {
        public List<ExternalMaintenanceConfig> Config { get; set; }
    }
    public class ExternalMaintenanceConfig
    {
        public string ClientId { get; set; }
        public bool IsMaintenance { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Site { get; set; }
    }
}
