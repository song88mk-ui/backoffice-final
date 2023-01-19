
namespace Backoffice.Shared.Models.Requests
{
    public class UserGroupListRequest
    {
        public string SearchText { get; set; }
        public bool ShowSuperAdmin { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
