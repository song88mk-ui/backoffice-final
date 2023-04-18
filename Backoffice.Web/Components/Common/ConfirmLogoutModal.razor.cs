using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class ConfirmLogoutModal : IDisposable
    {
        #region Properties

        [Inject] private INavMenuModalService ModalService { get; set; }

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
