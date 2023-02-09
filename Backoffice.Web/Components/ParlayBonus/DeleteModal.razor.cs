using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.ParlayBonus
{
    public partial class DeleteModal : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        public ParlayBonusConfig ParlayBonusConfig { get; set; }
        public string ClientName { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ParlayBonusConfig = ModalParameters.TryGet<ParlayBonusConfig>("ParlayBonusConfig");
            ClientName = ModalParameters.TryGet<string>("ClientName");
        }

        public void Dispose()
        {
            
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnApplyClickEvent()
        {
            await Modal.OnSaveClickCallback.InvokeAsync(ParlayBonusConfig);
            ModalService.Cancel();
        }

        #endregion
    }
}
