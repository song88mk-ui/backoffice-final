using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class DeleteTemplateModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        #endregion


        #region Methods
        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnApplyClickEvent()
        {
            ModalService.Cancel();
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
