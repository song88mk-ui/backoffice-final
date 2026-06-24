using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class AspNetRoles
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string NormalizedName { get; set; }
        public string ConcurrencyStamp { get; set; }
    }
}
