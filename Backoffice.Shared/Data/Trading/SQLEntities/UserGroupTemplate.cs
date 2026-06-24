using System;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class UserGroupTemplate
    {
        public int UserGroupTemplateId { get; set; }
        public int ParentId { get; set; }
        public string Name { get; set; }
        public int GroupId { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
