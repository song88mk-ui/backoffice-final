using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Maintenance
{
    public partial class ConfirmationModal
    {
        [Inject] IModalService ModalService { get; set; }
        public async Task OnSaveClickedEvent()
        {
            ModalService.Cancel();
        }
    }
}
