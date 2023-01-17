using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Clients
{
    public partial class ViewBetSettingsModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        #endregion

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public void Dispose()
        {

        }
    }
}
