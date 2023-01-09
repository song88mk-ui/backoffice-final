using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MarketGroups
{
    public partial class CopyMarketGroupModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        public ModalParameters ModalParameters { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            
        }

        public async Task OnModalCancelEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnModalCopyEvent()
        {
            ModalService.Cancel();
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
