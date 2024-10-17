
using Backoffice.Shared.Enums;
using Backoffice.Web.Models.Shared;
using Microsoft.JSInterop;

namespace Backoffice.Web.Shared
{
    public partial class MainLayout
    {
        protected override async Task OnInitializedAsync()
        {
            if (PreRenderService.IsPreRendering)
                return;

            NavMenuParameter = new NavMenuParameter()
            {
                NavMenuAccess = NavigationManager.Uri.Contains("/admin") ? PermissionCategory.Admin : PermissionCategory.SBC
            };

            base.InitializeState();
        }
    }
}
