using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MarketSortOrder
{
    public partial class CopyModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            
        }
        public async Task OnModalCancel()
        {
            ModalService.Cancel();
        }
        public async Task OnModalCopy()
        {
            ModalService.Cancel();
        }
        public void Dispose()
        {
            
        }
        #endregion
    }
}
