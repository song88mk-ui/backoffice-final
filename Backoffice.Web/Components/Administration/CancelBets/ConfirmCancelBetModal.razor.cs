using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Administration.CancelBets
{
    public partial class ConfirmCancelBetModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        public CancelBetModel CancelBetModel { get; set; }
        #endregion

        #region Methods

        public async Task OnCloseModalClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnConfirmCancelClickEvent()
        {
            ModalService.Cancel();

            await Modal.OnSaveClickCallback.InvokeAsync();
        }

        public void Dispose()
        {

        }
        #endregion
    }
}
