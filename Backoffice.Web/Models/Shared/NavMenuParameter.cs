using Backoffice.Shared.Enums;

namespace Backoffice.Web.Models.Shared
{
    public class NavMenuParameter
    {
        public PermissionCategory NavMenuAccess { get; set; }
        public NavMenuModel? NavMenus { get; set; }
    }
}
