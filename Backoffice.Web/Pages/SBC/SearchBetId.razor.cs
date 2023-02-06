using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class SearchBetId : IDisposable
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
            PageTitle = "Search Bet Id";
            IconClass = "ico ico-search-bet";

            Token = CommonService.GenerateToken();
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
