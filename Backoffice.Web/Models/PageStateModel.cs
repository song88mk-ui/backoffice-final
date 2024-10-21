using Backoffice.Shared.Enums;
using Backoffice.Web.Interfaces.Pages;

namespace Backoffice.Web.Models
{
    public class PageStateModel
    {
        public PermissionCategory NavMenuAccess { get; set; }
        public string? SportId { get; set; }
        public IPage Page { get; set; }
    }
}
