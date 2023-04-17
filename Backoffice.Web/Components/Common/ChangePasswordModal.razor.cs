using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class ChangePasswordModal : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }

        #endregion

        #region Methods

        public async Task OnCancelModalClickEvent()
        {
            ModalService.Cancel();
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
