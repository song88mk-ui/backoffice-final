using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Drawing;

namespace Backoffice.Web.Pages.SBC
{
    public partial class DrillDownReport : IDisposable
    {
        #region Properties

        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public ICommonService CommonService { get; set; }
        [Inject] public IConfiguration Configuration { get; set; }

        public ElementReference VizContainer { get; set; }

        public List<string> Clients { get; set; }
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
            PageTitle = "Drill Down Report";
            IconClass = "ico ico-drill-down-report";

            Token = CommonService.GenerateToken();

            Clients = new List<string>();
            Clients.Add("GURUPLAY");
            Clients.Add("10 BET JAPAN");

            await Initialize();
        }

        private async Task Initialize()
        {
            _region = Configuration["Region"].ToString();

            IFrameSrc = _region == "HK" ? "https://tableau.song88.com/t/song88/views/10bet/2_Reporting" : "";
        }

        [JSInvokable]
        public async Task OnFilterEvent()
        {

        }

        public void Dispose()
        {
        }

        #endregion
    }
}
