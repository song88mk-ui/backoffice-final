using Backoffice.Shared.Enums;
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

        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }

        [Parameter] public string Region { get; set; } = "hk";

        public IJSStreamReference? DataReference { get; set; }

        #endregion
        private string _tableClass;
        private string _selectClass;
        private string _selectedMarketGroupName;
        private bool _initSortingAndSelect;
        private bool _isMarketGroupClicked;
        private bool _isUpdate;
        private bool _isPreLoader;
        private PageHeaderModel _pageHeaderModel;
        private static MarketSortOrderModel _marketSortOrderModel;
        private List<MarketSortOrderModel.ClientMarketTemplate> _filteredMarketTemplates;
        private static List<SelectListItemModel<int>> _marketGroups;
        private List<BreadcrumbModel> _breadcrumbs;
        private AlertModel _alertModel;
        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _pageHeaderModel = new()
            {
                Title = "Market Sort Order"
            };
            TradingService.Region = Region.ToUpper();
            _marketSortOrderModel = new MarketSortOrderModel();

            _tableClass = "market-sort-order-table";
            _selectClass = "market-group-select";
            _selectedMarketGroupName = string.Empty;

            _initSortingAndSelect = false;
            _isMarketGroupClicked = false;
            _isUpdate = false;
            _isPreLoader = true;

            _alertModel = new();
            _breadcrumbs = new List<BreadcrumbModel>();

            _breadcrumbs.Add(new BreadcrumbModel
            {
                Name = "Market Settings",
                Url = string.Empty,
                IsCurrentPage = false
            });

            _breadcrumbs.Add(new BreadcrumbModel
            {
                Name = "Market Sort Order",
                Url = string.Empty,
                IsCurrentPage = true
            });

            await AuthenticateAccess.InvokeAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_initSortingAndSelect)
            {
                await JSRuntime.InvokeVoidAsync("initSort", $".{_tableClass}", ".drag-handle");
                await JSRuntime.InvokeVoidAsync("disableSort", $".{_tableClass}");

                await JSRuntime.InvokeVoidAsync("initSelect2", $".{_selectClass}", "OnMarketGroupChangedEvent");
                await JSRuntime.InvokeVoidAsync("disableSelect2", $".{_selectClass}");

                _initSortingAndSelect = false;
            }

            if (_isMarketGroupClicked)
            {
                await JSRuntime.InvokeVoidAsync("initSort", $".{_tableClass}", ".drag-handle");
                await JSRuntime.InvokeVoidAsync("initSelect2", $".{_selectClass}", "OnMarketGroupChangedEvent");

                if (_isUpdate)
                {
                    if (_selectedMarketGroupName.ToLower().Replace(" ", "").Trim() == "allmarkets")
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

                _isMarketGroupClicked = false;
            }
        }

        public async Task OnUpdateClickEvent()
        {
            _isUpdate = true;

            await EnableSortingAndSelect2();
        }

        public async Task OnCancelClickEvent()
        {
            _isUpdate = false;

            await DisableSortingAndSelect2();
        }

        public async Task OnSaveClickEvent()
        {
            _isUpdate = false;

            if (!_isPreLoader)
            {
                _isPreLoader = true;
            }

            var marketTemplates = await GetMarketTemplates();

            if (!marketTemplates.Any())
                return;

            _marketSortOrderModel.MarketTemplates = _marketSortOrderModel.MarketTemplates.Join(marketTemplates,
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
                ClientId = _marketSortOrderModel.ClientId,
                SportId = _marketSortOrderModel.SportId,
                FixtureStatusId = _marketSortOrderModel.FixtureStatusId,
                MarketTemplates = _marketSortOrderModel.MarketTemplates.Select(s => new ClientMarketTemplateModel
                {
                    MarketTemplateId = s.MarketTemplateId,
                    MarketTemplateName = s.MarketTemplateName,
                    MarketTemplateGroupNames = s.MarketTemplateGroupNames,
                    SortOrder = s.SortOrder
                }).ToList()
            };

            await InvokeAsync(StateHasChanged);

            var response = await TradingService.AddOrUpdateClientMarketTemplatesOrder(request);

            if (response.StatusCode != StatusCode.Success)
            {
                _alertModel = new()
                {
                    Status = AlertStatus.Error,
                    Message = response.Message
                };
            }

            _alertModel = new()
            {
                Status = AlertStatus.Success,
                Message = AlertStatus.Success.ToString()
            };

            _isPreLoader = false;
        }

        public async Task OnMarketGroupClickEvent(string name)
        {
            _selectedMarketGroupName = name;
            _filteredMarketTemplates = _marketSortOrderModel.MarketTemplates;

            if (_selectedMarketGroupName.ToLower() != "all markets")
            {
                _filteredMarketTemplates = _filteredMarketTemplates.Where(c => c.MarketTemplateGroupNames.Select(s => s.ToLower()).Contains(_selectedMarketGroupName.ToLower())).ToList();
            }

            _isMarketGroupClicked = true;
        }

        public async Task PrepareMarketSortOrderModel(MarketSortOrderModel model)
        {
            _marketSortOrderModel = model;

            _filteredMarketTemplates = _marketSortOrderModel.MarketTemplates;
            _selectedMarketGroupName = "All Markets";

            _marketGroups = _marketSortOrderModel.MarketTemplateGroups
                .Where(s => s.Name.ToLower() != _selectedMarketGroupName.ToLower())
                .Select(s => new SelectListItemModel<int>
                {
                    Value = s.MarketTemplateGroupId,
                    Name = s.Name
                }).ToList();

            // set InitMultipleSelect to true to enable multiple select2.
            _initSortingAndSelect = true;
        }

        [JSInvokable("OnMarketGroupChangedEvent")]
        public static void OnMarketGroupChangedEvent(SelectedModel model)
        {
            var marketGroupId = int.Parse(model.SelectedOptionId);
            var marketGroup = _marketGroups.FirstOrDefault(c => c.Value == marketGroupId);

            if (marketGroup != null)
            {
                var marketTemplateId = model.ParentId.Split('-')[1];

                if (model.IsSelect)
                {
                    foreach (var market in _marketSortOrderModel.MarketTemplates.Where(c => c.MarketTemplateId == marketTemplateId))
                    {
                        if (!market.MarketTemplateGroupNames.Select(s => s.ToLower()).Contains(marketGroup.Name.ToLower()))
                            market.MarketTemplateGroupNames.Add(marketGroup.Name);
                    }
                }
                else
                {
                    foreach (var market in _marketSortOrderModel.MarketTemplates.Where(c => c.MarketTemplateId == marketTemplateId))
                    {
                        if (market.MarketTemplateGroupNames.Select(s => s.ToLower()).Contains(marketGroup.Name.ToLower()))
                            market.MarketTemplateGroupNames.Remove(marketGroup.Name);
                    }
                }
            }
        }

        public async Task OnModalDiscardChanges()
        {
            _marketSortOrderModel.MarketTemplates = new List<MarketSortOrderModel.ClientMarketTemplate>();
            _filteredMarketTemplates = new List<MarketSortOrderModel.ClientMarketTemplate>();

            await InvokeAsync(StateHasChanged);
            await Task.Delay(500);

            _marketSortOrderModel.MarketTemplates = _marketSortOrderModel.OriginalMarketTemplates;
            _filteredMarketTemplates = _marketSortOrderModel.MarketTemplates;

            if (_selectedMarketGroupName.ToLower() != "all markets")
            {
                _filteredMarketTemplates = _filteredMarketTemplates.Where(c => c.MarketTemplateGroupNames.Select(s => s.ToLower()).Contains(_selectedMarketGroupName.ToLower())).ToList();
            }

            _initSortingAndSelect = true;
            _isUpdate = false;
        }

        public async Task OnSaveCopyClickEvent()
        {
            _alertModel = new()
            {
                Status = AlertStatus.Success,
                Message = "Market Sort Order settings copied successfully."
            };
        }

        public async ValueTask DisposeAsync()
        {

            if (DataReference is not null)
                await DataReference.DisposeAsync();

            DataReference = null;

            //GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            TradingService.Dispose();
            //GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        private async Task EnableSortingAndSelect2()
        {
            await JSRuntime.InvokeVoidAsync("enableSort", $".{_tableClass}");
            await JSRuntime.InvokeVoidAsync("enableSelect2", $".{_selectClass}");
        }

        private async Task DisableSortingAndSelect2()
        {
            await JSRuntime.InvokeVoidAsync("disableSort", $".{_tableClass}");
            await JSRuntime.InvokeVoidAsync("disableSelect2", $".{_selectClass}");
        }

        private async Task<List<SortOrderModel>> GetMarketTemplates()
        {
            var marketTemplates = new List<SortOrderModel>();

            DataReference = await JSRuntime.InvokeAsync<IJSStreamReference>("getSortOrder", $".{_tableClass}");

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
