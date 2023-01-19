using Backoffice.Shared.Enums;

namespace Backoffice.Web.Models
{
    public class ManageUserFilterModel
    {
        public string SearchText { get; set; } = "";
        public UserSorting UserSorting { get; set; }
    }
}
