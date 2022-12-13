using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.ParlayBonus
{
    public partial class DeleteModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {

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
            ModalService.Cancel();
        }
        #endregion
    }
}
