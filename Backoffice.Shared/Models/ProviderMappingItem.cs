using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class ProviderMappingItem
    {
        public string Id { get; set; }
        public string Na { get; set; }
        public ProviderModel<string> Info { get; set; }
        public int? SpId { get; set; } = null;
        public int? SegId { get; set; } = null;
        public int? LeId { get; set; } = null;
    }
}
