using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class PriceBoostConfigAuditModel
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public string SelectionTemplateId { get; set; }
        public string SelectionSpecifier { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime TimeStamp { get; set; }
        public string ChangesMade { get; set; }
    }
}
