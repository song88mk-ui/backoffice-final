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
        public string SelectClass { get; set; }
        public bool InitSortingAndSelect2 { get; set; }
        public MarketSortOrderModel MarketSortOrderModel { get; set; }
        public List<SelectListItemModel<int>> MarketGroups { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            MarketSortOrderModel = new MarketSortOrderModel();

            PageTitle = "Market Sort Order";
            IconClass = string.Empty;
            TableClass = "market-sort-order-table";
            SelectClass = "market-group-select";

            InitSortingAndSelect2 = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (InitSortingAndSelect2)
            {
                await JSRuntime.InvokeVoidAsync("initSort", $".{TableClass}", ".drag-handle");
                await JSRuntime.InvokeVoidAsync("disableSort", $".{TableClass}");

                await JSRuntime.InvokeVoidAsync("initSelect2", $".{SelectClass}");
                await JSRuntime.InvokeVoidAsync("disableSelect2", $".{SelectClass}");

                InitSortingAndSelect2 = false;
            }
        }

        public async Task OnUpdateClickEvent()
        {
            await JSRuntime.InvokeVoidAsync("enableSort", $".{TableClass}");
            await JSRuntime.InvokeVoidAsync("enableSelect2", $".{SelectClass}");
        }

        public async Task OnCancelClickEvent()
        {
            await JSRuntime.InvokeVoidAsync("disableSort", $".{TableClass}");
            await JSRuntime.InvokeVoidAsync("disableSelect2", $".{SelectClass}");
        }

        public async Task PrepareMarketSortOrderModel(MarketSortOrderModel model)
        {
            MarketSortOrderModel = model;

            MarketGroups = MarketSortOrderModel.MarketTemplateGroups
                .Where(s => s.Name.ToLower() != "all markets")
                .Select(s => new SelectListItemModel<int>
                {
                    Value = s.MarketTemplateGroupId,
                    Name = s.Name
                }).ToList();

            // set InitMultipleSelect to true to enable multiple select2.
            InitSortingAndSelect2 = true;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
