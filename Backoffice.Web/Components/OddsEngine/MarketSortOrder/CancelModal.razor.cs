using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MarketSortOrder
{
    public partial class CancelModal : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] private CustomModal Modal { get; set; }

        public string ClientName { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ClientName = ModalParameters.TryGet<string>("ClientName");
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }
        public async Task OnContinueClickEvent()
        {
            ModalService.Cancel();
            await Modal.OnCancelClickCallback.InvokeAsync();
        }
        public void Dispose()
        {

        }

        #endregion
    }
}
