
using Microsoft.JSInterop;

namespace Backoffice.Web.Shared
{
    public partial class MainLayout
    {
        protected override async Task OnInitializedAsync()
        {
            if (PreRenderService.IsPreRendering)
                return;

            base.InitializeState();
            await Task.CompletedTask;
        }
    }
}
