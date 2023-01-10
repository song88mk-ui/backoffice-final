using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MarketSettlement
{
    public partial class AddMarketSettlementModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            
        }
        public void Dispose()
        {
            
        }
        #endregion
    }
}
