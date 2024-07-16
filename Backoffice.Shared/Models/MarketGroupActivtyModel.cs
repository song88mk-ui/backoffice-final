using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class MarketGroupActivtyModel
    {
        public int MarketTemplateGroupId { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public string MarketTemplateIds { get; set; }
        public bool IsActive { get; set; }
    }
}
