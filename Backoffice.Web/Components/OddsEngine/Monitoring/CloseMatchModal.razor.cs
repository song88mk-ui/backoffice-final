using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Monitoring
{
    public partial class CloseMatchModal : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }

        #endregion        

        #region Methods

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnConfirmClickEvent()
        {

        }

        public void Dispose()
        {

        }

        #endregion        
    }
}
