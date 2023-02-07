namespace Backoffice.Shared.Models
{
    public class PermissionModel
    {
        public string Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
        public string GroupName { get; set; }
        public string Name { get; set; }
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }
        public bool HasReadOnly { get; set; }
        public int SortOrder { get; set; }
        public bool IsSubUser { get; set; }
        public bool IsEnabled { get; set; } = false;
    }
}
