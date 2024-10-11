using System.Collections.Generic;

namespace Backoffice.Shared.Models
{
    public class PermissionTreeModel
    {
        public PermissionTreeModel()
        {
            Permissions = new List<PermissionCategoryModel>();
        }

        public List<PermissionCategoryModel> Permissions { get; set; }

        public class PermissionCategoryModel
        {
            public PermissionCategoryModel()
            {
                SubCategories = new List<SubCategory>();
            }

            public int CategoryId { get; set; }
            public string CategoryName { get; set; }
            public bool IsEnabled { get; set; }
            public List<SubCategory> SubCategories { get; set; }
        }

        public class SubCategory
        {
            public SubCategory()
            {
                Groups = new List<Group>();
                Claims = new List<PermissionClaim>();
            }
            public string SubCategoryName { get; set; }
            public int Availability { get; set; } = 1;
            public List<Group> Groups { get; set; }
            public List<PermissionClaim> Claims { get; set; }
            public string ColPosition { get; set; }
        }

        public class Group
        {
            public Group()
            {
                Claims = new List<PermissionClaim>();
            }
            public string GroupName { get; set; }
            public List<PermissionClaim> Claims { get; set; }
        }
        public class PermissionClaim
        {
            public string Name { get; set; }
            public string ClaimType { get; set; }
            public bool HasReadOnly { get; set; }
            public int SortOrder { get; set; }
            public bool IsSubUser { get; set; }
            public string ClaimValue { get; set; }
            public int Availability { get; set; }
            public bool IsReadOnly { get; set; }
        }
    }

}
