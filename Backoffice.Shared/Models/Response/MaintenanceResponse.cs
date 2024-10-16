using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class MaintenanceResponse
    {
        public List<string> Live { get; set; }
        public List<string> UnderMaintenance { get; set; }
    }
}
