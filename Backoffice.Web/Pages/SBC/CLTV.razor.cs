using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class CLTV : IDisposable
    {
        #region Properties

        [Inject] public ICommonService CommonService { get; set; }
        [Inject] public IConfiguration Configuration { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public string Token { get; set; }
        public string IFrameSrc { get; set; }

        #endregion

        #region Fields

        private string _region;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "CLTV";
            IconClass = "ico ico-cltv";

            Token = CommonService.GenerateToken();

            await Initialize();
        }

        private async Task Initialize()
        {
            _region = Configuration["Region"].ToString();

            IFrameSrc = _region == "HK" ? "https://tableau.song88.com/t/song88/views/10bet/9_LTV_" : "";
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
