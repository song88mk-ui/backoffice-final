using Backoffice.Web.Components.OddsEngine.ManageBoost;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MarketSettlement
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        public bool ShowFilter { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            ShowFilter = false;
        }
        public async Task OnFilterClickEvent()
        {
            ShowFilter = !ShowFilter;
        }

        public async Task OnAddMarketSettlementClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle","max-width: 100rem");

            var options = new ModalOptions();
            ModalService.Open<AddMarketSettlementModal>($"Add Market Settlement", parameters, options);
        }
        public void Dispose()
        {
            
        }
        #endregion
    }
}
