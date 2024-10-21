using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class SetMaintenanceRequest : GetMaintenanceRequest
    {
        public bool IsMaintenance { get; set; }
    }
    public class GetMaintenanceRequest
    {
        public string ClientIds { get; set; }
    }
}
