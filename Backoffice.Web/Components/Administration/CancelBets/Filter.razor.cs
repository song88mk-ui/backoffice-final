using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Administration.CancelBets
{
    public partial class Filter
    {
        #region Properties
        [Parameter] public EventCallback OnAddBetReferenceIdCallback { get; set; }
        [Parameter] public EventCallback OnCancelBetCallback { get; set; }
        [Parameter] public EventCallback OnResetCallback { get; set; }
        #endregion

        #region Methods
        protected override Task OnInitializedAsync()
        {
            return base.OnInitializedAsync();
        }

        public async Task OnAddBetReferenceIdCallbackEvent()
        {
            await OnAddBetReferenceIdCallback.InvokeAsync();
        }
        public async Task OnCancelBetCallbackEvent()
        {
            await OnCancelBetCallback.InvokeAsync();
        }
        public async Task OnResetCallbackEvent()
        {
            await OnResetCallback.InvokeAsync();
        }
        #endregion
    }
}
