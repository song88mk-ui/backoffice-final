namespace Backoffice.Shared.Models
{
    public class PermissionModel
    {
        public string Category { get; set; }
        public string Name { get; set; }
        public string ClaimType { get; set; }
        public bool HasReadOnly { get; set; }
        public int SortOrder { get; set; }
        public bool IsSubUser { get; set; }
    }
}
