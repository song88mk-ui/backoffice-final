using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MarketGroups
{
    public partial class AddOrEditMarketGroupModal : IDisposable
    {
        #region Properties
        [Inject] IModalService ModalService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        public bool IsEdit { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsEdit = ModalParameters.TryGet<bool>("IsEdit");
        }

        public async Task OnModalCancelEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnModalSaveEvent()
        {
            ModalService.Cancel();
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
