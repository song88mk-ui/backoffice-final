using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Pages.SBC
{
    public partial class Oddin
    {
        #region Properties

        [Inject] public IJSRuntime JSRuntime { get; set; }

        public OddinConfig Config { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            Config = new OddinConfig();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
        }

        public async void LoadOddin()
        {
            if (!string.IsNullOrEmpty(Config.ClientId) && !string.IsNullOrEmpty(Config.OperatorToken) && !string.IsNullOrEmpty(Config.CurrencyCode))
            {
                await JSRuntime.InvokeVoidAsync("buildESport", $"{Config.ClientId.ToUpper()}|{Config.OperatorToken.ToUpper()}", Config.CurrencyCode.ToUpper());
            }
        }

        #endregion
    }
}
