using Microsoft.AspNetCore.Components;
using Backoffice.Web.Services;

namespace Backoffice.Web.Components.OddsEngine.Monitoring
{
    public partial class ConfirmSelectionModal: IDisposable
    {

        [Inject] public IModalService ModalService { get; set; }

        public void OnCloseClickEvent()
        {
            ModalService.Cancel();
        }
        public void Dispose()
        {
        }
    }
}
