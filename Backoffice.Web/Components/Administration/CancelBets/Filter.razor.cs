using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Administration.CancelBets
{
    public partial class Filter
    {
        #region Properties
        [Parameter] public EventCallback OnAddBetReferenceIdCallback { get; set; }
        [Parameter] public EventCallback OnCancelBetCallback { get; set; }
        [Parameter] public EventCallback OnResetCallback { get; set; }
        [Parameter] public bool IsPreload { get; set; }
        #endregion

        #region Methods
        protected override Task OnInitializedAsync()
        {
            return base.OnInitializedAsync();
        }

        public async Task OnAddBetReferenceIdCallbackEvent()
        {
            if (IsPreload)
                return;

            await OnAddBetReferenceIdCallback.InvokeAsync();
        }
        public async Task OnCancelBetCallbackEvent()
        {
            if (IsPreload)
                return;

            await OnCancelBetCallback.InvokeAsync();
        }
        public async Task OnResetCallbackEvent()
        {
            if (IsPreload)
                return;

            await OnResetCallback.InvokeAsync();
        }
        #endregion
    }
}
