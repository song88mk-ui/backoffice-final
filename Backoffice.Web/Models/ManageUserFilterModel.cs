using Backoffice.Shared.Enums;

namespace Backoffice.Web.Models
{
    public class ManageUserFilterModel
    {
        public string Search { get; set; } = "";
        public UserSorting UserSorting { get; set; }
    }
}
