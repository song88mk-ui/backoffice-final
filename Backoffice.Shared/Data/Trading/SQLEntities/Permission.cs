using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class Permission
    {
        public Guid PermissionId { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string ClaimType { get; set; }

        public bool HasReadOnly { get; set; } = false;
        public int SortOrder { get; set; }
    }
}
