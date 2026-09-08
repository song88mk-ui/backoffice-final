using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class AspNetRoleClaims
    {
        public int Id { get; set; }
        public string RoleId { get; set; }
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }

    }
}
