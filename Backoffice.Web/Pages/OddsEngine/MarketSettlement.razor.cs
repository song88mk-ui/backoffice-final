using Backoffice.Shared.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MarketSettlement : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        public MetaData MetaData { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 1
            };

            PageTitle = "Market Settlement";
            IconClass = "";
        }

        public async Task SelectPage()
        {

        }
        public void Dispose()
        {
            
        }
        #endregion
    }
}
