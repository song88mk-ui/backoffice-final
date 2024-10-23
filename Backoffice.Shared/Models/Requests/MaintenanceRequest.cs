using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class SetMaintenanceRequest
    {
        public string ClientIds { get; set; }
        public bool IsMaintenance { get; set; }
    }
}
