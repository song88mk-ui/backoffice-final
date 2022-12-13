using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Pages
{
    public partial class Dashboard : IDisposable
    {
        #region Properties

        [Inject] public IJSRuntime JSRuntime { get; set; }

        public ElementReference VizContainer { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Dashboard";
            IconClass = "ico-dashboard";
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var url = "https://tableau.song88.com/t/song88/views/design/Dashboard1?:showAppBanner=false&:toolbar=no";
                await JSRuntime.InvokeVoidAsync("initViz", VizContainer, url);
            }
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
