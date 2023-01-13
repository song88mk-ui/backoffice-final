using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MarketSortOrder : IDisposable
    {
        #region Properties

        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public ITradingService TradingService { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public string TableClass { get; set; }
        public MarketSortOrderModel MarketSortOrderModel { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            MarketSortOrderModel = new MarketSortOrderModel();

            PageTitle = "Market Sort Order";
            IconClass = string.Empty;

            TableClass = "market-sort-order-table";
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            //if (firstRender)
            //{
            //    await JSRuntime.InvokeVoidAsync("initSort", $".{TableClass}", ".drag-handle");
            //    await JSRuntime.InvokeVoidAsync("disableSort", $".{TableClass}");
            //}
        }

        public async Task OnUpdateClickEvent()
        {
            await JSRuntime.InvokeVoidAsync("initSort", $".{TableClass}", ".drag-handle");
            await JSRuntime.InvokeVoidAsync("enableSort", $".{TableClass}");
        }

        public async Task OnCancelClickEvent()
        {
            await JSRuntime.InvokeVoidAsync("disableSort", $".{TableClass}");
        }

        public async Task PrepareMarketSortOrderModel(MarketSortOrderModel model)
        {
            MarketSortOrderModel = model;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
