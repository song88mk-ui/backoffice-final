using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class Performance : IDisposable
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
            PageTitle = "Performance";
            IconClass = "ico ico-performance";

            Token = CommonService.GenerateToken();
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
