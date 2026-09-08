using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.SBC.ParlayBonus
{
    public partial class ViewLogModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        public ParlayBonusConfig ParlayBonusConfig { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ParlayBonusConfig = ModalParameters.TryGet<ParlayBonusConfig>("ParlayBonusConfig");
        }

        public async Task OnCloseClickEvent()
        {
            ModalService.Cancel();
        }

        public void Dispose()
        {

        }
        #endregion
    }
}
