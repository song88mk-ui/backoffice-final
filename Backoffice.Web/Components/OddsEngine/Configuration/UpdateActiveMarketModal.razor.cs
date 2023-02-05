using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class UpdateActiveMarketModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        private EventCallback OnCancelCallback { get; set; }
        
        public int MarketChangedCount { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            OnCancelCallback = ModalParameters.TryGet<EventCallback>("OnCancelCallback");
            MarketChangedCount = ModalParameters.TryGet<int>("MarketChangedCount");
        }

        public async Task OnBackClickedEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnClearClickedEvent()
        {
            ModalService.Cancel();

            await OnCancelCallback.InvokeAsync();
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
