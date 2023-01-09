using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MarketGroups
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Parameter] public EventCallback OnAddMarketGroupCallback { get; set; }
        [Parameter] public EventCallback OnCopyMarketGroupCallback { get; set; }
        [Parameter] public EventCallback OnViewLogsCallback { get; set; }
        public bool IsUpdate { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            IsUpdate = false;
        }

        public async Task OnUpdateClickEvent()
        {
            IsUpdate = true;
        }

        public async Task OnCancelClickEvent()
        {
            IsUpdate = false;
        }

        public async Task OnAddMarketGroupClickEvent()
        {
            await OnAddMarketGroupCallback.InvokeAsync();
        }

        public async Task OnCopyMarketGroupClickEvent()
        {
            await OnCopyMarketGroupCallback.InvokeAsync();
        }

        public async Task OnViewLogsClickEvent()
        {
            await OnViewLogsCallback.InvokeAsync();
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
