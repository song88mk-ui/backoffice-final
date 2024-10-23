using Backoffice.Shared.ExternalApis.Maintenance;

namespace Backoffice.Shared.Models
{
    public class MaintenanceConfigModel : ExternalMaintenanceConfig
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string App { get; set; }
    }
}
