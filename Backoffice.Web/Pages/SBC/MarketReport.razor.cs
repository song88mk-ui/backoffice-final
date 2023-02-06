using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class MarketReport : IDisposable
    {
        #region Properties

        [Inject] public ICommonService CommonService { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public string Token { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Market Report";
            IconClass = "ico ico-market-report";

            Token = CommonService.GenerateToken();
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
