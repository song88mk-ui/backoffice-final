using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class BoostedSettings
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        [Column(TypeName = "decimal(9,6)")]
        public decimal Cap { get; set; }
    }
}
