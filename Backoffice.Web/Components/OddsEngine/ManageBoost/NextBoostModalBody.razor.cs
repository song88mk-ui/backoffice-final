using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.ManageBoost
{
    public partial class NextBoostModalBody : IDisposable
    {
        #region Properties
        [Inject] IModalService ModalService { get; set; }
        #endregion
        #region Methods
        public async Task OnCancelModalClick()
        {
            ModalService.Cancel();
        }

        public async Task OnSaveModalClick()
        {

        }
        public void Dispose()
        {
            
        }
        #endregion
    }
}
