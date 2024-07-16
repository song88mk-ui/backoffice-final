using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class MaintenanceModel
    {
        public string ClientId { get; set; }
        public bool IsMaintenance { get; set; }
        public string Site { get; set; }
    }
}
