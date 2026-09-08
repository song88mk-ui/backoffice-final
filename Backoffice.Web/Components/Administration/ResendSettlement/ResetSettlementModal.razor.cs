using Backoffice.Web.Components.Common;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Administration.ResendSettlement
{
    public partial class ResetSettlementModal : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }

        [CascadingParameter] private CustomModal Modal { get; set; }

        #endregion

        #region Methods

        public async Task OnModalCancel()
        {
            ModalService.Cancel();
        }

        public async Task OnModalReset()
        {
            await Modal.OnSaveClickCallback.InvokeAsync();
            ModalService.Cancel();
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
