using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text;
using System.Text.Json;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MarketSortOrder : IAsyncDisposable, IDisposable
    {
        #region Properties

        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public ITradingService TradingService { get; set; }

        public IJSStreamReference? DataReference { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public string TableClass { get; set; }
        public string SelectClass { get; set; }
        public string SelectedMarketGroupName { get; set; }

        public bool InitSortingAndSelect2 { get; set; }
        public bool IsMarketGroupClicked { get; set; }        
        public bool IsUpdate { get; set; }

        public static MarketSortOrderModel MarketSortOrderModel { get; set; }
        public static List<MarketSortOrderModel.ClientMarketTemplate> FilteredMarketTemplates { get; set; }
        public static List<SelectListItemModel<int>> MarketGroups { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            MarketSortOrderModel = new MarketSortOrderModel();

            PageTitle = "Market Sort Order";
            IconClass = string.Empty;
            TableClass = "market-sort-order-table";
            SelectClass = "market-group-select";
            SelectedMarketGroupName = string.Empty;

            InitSortingAndSelect2 = false;
            IsMarketGroupClicked = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (InitSortingAndSelect2)
            {
                await JSRuntime.InvokeVoidAsync("initSort", $".{TableClass}", ".drag-handle");
                await JSRuntime.InvokeVoidAsync("disableSort", $".{TableClass}");

                await JSRuntime.InvokeVoidAsync("initSelect2", $".{SelectClass}", "OnMarketGroupChangedEvent");
                await JSRuntime.InvokeVoidAsync("disableSelect2", $".{SelectClass}");

                InitSortingAndSelect2 = false;
            }

            if (IsMarketGroupClicked)
            {
                await JSRuntime.InvokeVoidAsync("initSort", $".{TableClass}", ".drag-handle");
                await JSRuntime.InvokeVoidAsync("initSelect2", $".{SelectClass}", "OnMarketGroupChangedEvent");

                if (IsUpdate)
                {
                    if (SelectedMarketGroupName.ToLower() == "all markets")
                    {
                        await EnableSortingAndSelect2();
                    }
                    else
                    {
                        await DisableSortingAndSelect2();
                    }
                }
                else
                {
                    await DisableSortingAndSelect2();
                }

                IsMarketGroupClicked = false;
            }
        }

        public async Task OnUpdateClickEvent()
        {
            IsUpdate = true;

            await EnableSortingAndSelect2();
        }

        public async Task OnCancelClickEvent()
        {
            IsUpdate= false;

            await DisableSortingAndSelect2();
        }

        public async Task OnSaveClickEvent()
        {
            IsUpdate = false;

            var marketTemplates = await GetMarketTemplates();

            if (!marketTemplates.Any())
                return;

            MarketSortOrderModel.MarketTemplates = MarketSortOrderModel.MarketTemplates.Join(marketTemplates,
                ma => ma.MarketTemplateId,
                so => so.RowId.Split('-')[1],
                (ma, so) => new MarketSortOrderModel.ClientMarketTemplate
                {
                    MarketTemplateId = ma.MarketTemplateId,
                    MarketTemplateName = ma.MarketTemplateName,
                    MarketTemplateGroupNames = ma.MarketTemplateGroupNames,
                    SortOrder = so.SortOrder
                })
                .OrderBy(o => o.SortOrder).ToList();

            var request = new ClientMarketTemplateRequest
            {
                ClientId = MarketSortOrderModel.ClientId,
                SportId = MarketSortOrderModel.SportId,
                FixtureStatusId = MarketSortOrderModel.FixtureStatusId,
                MarketTemplates = MarketSortOrderModel.MarketTemplates.Select(s => new ClientMarketTemplateModel
                {
                    MarketTemplateId = s.MarketTemplateId,
                    MarketTemplateName = s.MarketTemplateName,
                    MarketTemplateGroupNames = s.MarketTemplateGroupNames,
                    SortOrder = s.SortOrder
                }).ToList()
            };

            var response = await TradingService.AddOrUpdateClientMarketTemplatesOrder(request);
        }

        public async Task OnMarketGroupClickEvent(string name)
        {
            SelectedMarketGroupName = name;
            FilteredMarketTemplates = MarketSortOrderModel.MarketTemplates;

            if (SelectedMarketGroupName.ToLower() != "all markets")
            {
                FilteredMarketTemplates = FilteredMarketTemplates.Where(c => c.MarketTemplateGroupNames.Select(s => s.ToLower()).Contains(SelectedMarketGroupName.ToLower())).ToList();
            }

            IsMarketGroupClicked = true;
        }

        public async Task PrepareMarketSortOrderModel(MarketSortOrderModel model)
        {
            MarketSortOrderModel = model;

            FilteredMarketTemplates = MarketSortOrderModel.MarketTemplates;
            SelectedMarketGroupName = "All Markets";

            MarketGroups = MarketSortOrderModel.MarketTemplateGroups
                .Where(s => s.Name.ToLower() != SelectedMarketGroupName.ToLower())
                .Select(s => new SelectListItemModel<int>
                {
                    Value = s.MarketTemplateGroupId,
                    Name = s.Name
                }).ToList();

            // set InitMultipleSelect to true to enable multiple select2.
            InitSortingAndSelect2 = true;
        }

        [JSInvokable("OnMarketGroupChangedEvent")]
        public static void OnMarketGroupChangedEvent(SelectedModel model)
        {
            var marketGroupId = int.Parse(model.SelectedOptionId);
            var marketGroup = MarketGroups.FirstOrDefault(c => c.Value == marketGroupId);

            if (marketGroup != null)
            {
                var marketTemplateId = model.ParentId.Split('-')[1];

                if (model.IsSelect)
                {
                    foreach (var market in MarketSortOrderModel.MarketTemplates.Where(c => c.MarketTemplateId == marketTemplateId))
                    {
                        if (!market.MarketTemplateGroupNames.Select(s => s.ToLower()).Contains(marketGroup.Name.ToLower()))
                            market.MarketTemplateGroupNames.Add(marketGroup.Name);
                    }
                }
                else
                {
                    foreach (var market in MarketSortOrderModel.MarketTemplates.Where(c => c.MarketTemplateId == marketTemplateId))
                    {
                        if (market.MarketTemplateGroupNames.Select(s => s.ToLower()).Contains(marketGroup.Name.ToLower()))
                            market.MarketTemplateGroupNames.Remove(marketGroup.Name);
                    }
                }
            }
        }

        public async Task OnModalDiscardChanges()
        {
            await OnCancelClickEvent();
        }

        public async ValueTask DisposeAsync()
        {

            if (DataReference is not null)
                await DataReference.DisposeAsync();

            DataReference = null;

            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        private async Task EnableSortingAndSelect2()
        {
            await JSRuntime.InvokeVoidAsync("enableSort", $".{TableClass}");
            await JSRuntime.InvokeVoidAsync("enableSelect2", $".{SelectClass}");
        }

        private async Task DisableSortingAndSelect2()
        {
            await JSRuntime.InvokeVoidAsync("disableSort", $".{TableClass}");
            await JSRuntime.InvokeVoidAsync("disableSelect2", $".{SelectClass}");
        }

        private async Task<List<SortOrderModel>> GetMarketTemplates()
        {
            var marketTemplates = new List<SortOrderModel>();

            DataReference = await JSRuntime.InvokeAsync<IJSStreamReference>("getSortOrder", $".{TableClass}");

            using (var stream = await DataReference.OpenReadStreamAsync(maxAllowedSize: 10_000_000))
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    var hasLine = true;

                    while (hasLine)
                    {
                        var content = await reader.ReadLineAsync();

                        if (content == null)
                        {
                            hasLine = false;
                            continue;
                        }

                        marketTemplates.Add(JsonSerializer.Deserialize<SortOrderModel>(content));
                    }
                }
            }

            return marketTemplates;
        }

        #endregion
    }
}
