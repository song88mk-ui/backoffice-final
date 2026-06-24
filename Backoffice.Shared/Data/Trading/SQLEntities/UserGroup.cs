using System;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class UserGroup
    {
        public Guid UserGroupId { get; set; }
        public string Name { get; set; }
        public string Permission { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
