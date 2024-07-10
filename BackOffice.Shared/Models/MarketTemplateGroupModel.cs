using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class MarketTemplateGroupModel
    {
        public int MarketTemplateGroupId { get; set; }
        public string MarketTemplateGroupName { get; set; }
        public int SortOrder { get; set; }
        public List<string> MarketTemplateIds { get; set; }
    }
}
