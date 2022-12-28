using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.ManageBoost
{
    public partial class ViewLogsBoostedModal : IDisposable
    {
        #region Properties
        [Inject] IModalService ModalService { get; set; }
        #endregion
        #region Methods
        public async Task OnCancelModalClick()
        {
            ModalService.Cancel();
        }
        public void Dispose()
        {
            
        }
        #endregion
    }
}
