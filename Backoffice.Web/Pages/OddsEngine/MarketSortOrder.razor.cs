using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RabbitMQ.Client.Apigen.Attributes;
using System.Text;
using System.Text.Json;
using static Backoffice.Web.Models.MarketSortOrderModel;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MarketSortOrder : IAsyncDisposable, IDisposable
    {
        #region Properties

        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public ITradingService TradingService { get; set; }

        

        [Parameter] public string Region { get; set; } = "hk";

        public IJSStreamReference? DataReference { get; set; }

        #endregion
        private string _tableClass;
        private string _selectClass;
        private bool _initSortingAndSelect;
        private bool _isMarketGroupClicked;
        private bool _isUpdate;
        private bool _isPreLoader;
        private bool _isMarketPreLoader;
        private PageHeaderModel _pageHeaderModel;
        private MarketSortOrderFilterModel _filterModel = new();
        private List<ClientMarketTemplate> _filteredMarketTemplatesRef;
        private static List<ClientMarketTemplate> _marketTemplates;
        public List<ClientMarketTemplateGroup> _clientMarketTemplateGroupsRef;
        private static List<SelectListItemModel<int>> _marketGroups;
        private List<BreadcrumbModel> _breadcrumbs;
        private AlertModel _alertModel; 
        private int _selectedMarketGroupId;
        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _pageHeaderModel = new()
            {
                Title = "Market Sort Order",
                ShowLanguageDropDown = false
            };
            TradingService.Region = Region.ToUpper();

            _tableClass = "market-sort-order-table";
            _selectClass = "market-group-select";

            _initSortingAndSelect = false;
            _isMarketGroupClicked = false;
            _isUpdate = false;
            _isPreLoader = true;
            _isMarketPreLoader = false;

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

            await PageState.AuthenticateAccessEvent.InvokeAsync();
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
                    if (_selectedMarketGroupId == 1)
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

            if (_selectedMarketGroupId == 1)
                await EnableSortingAndSelect2();
            else 
                await DisableSortingAndSelect2();
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

            _marketTemplates = _marketTemplates.Join(marketTemplates,
                ma => ma.MarketTemplateId,
                so => so.RowId.Split('-')[1],
                (ma, so) => new ClientMarketTemplate
                {
                    MarketTemplateId = ma.MarketTemplateId,
                    MarketTemplateName = ma.MarketTemplateName,
                    MarketTemplateGroupNames = ma.MarketTemplateGroupNames,
                    SortOrder = so.SortOrder
                })
                .OrderBy(o => o.SortOrder).ToList();

            var request = new ClientMarketTemplateRequest
            {
                ClientId = new Guid(_filterModel.ClientId),
                ClientCode = _filterModel.SelectedClientCode,
                SportId = _filterModel.SelectedSportId,
                FixtureStatusId = _filterModel.SelectedFixtureStatusId,
                MarketTemplates = _marketTemplates.Select(s => new ClientMarketTemplateModel
                {
                    MarketTemplateId = s.MarketTemplateId,
                    MarketTemplateName = s.MarketTemplateName,
                    MarketTemplateGroupNames = s.MarketTemplateGroupNames,
                    SortOrder = s.SortOrder
                }).ToList()
            };

            var response = await TradingService.AddOrUpdateClientMarketTemplatesOrder(request);

            _isPreLoader = false;
            if (response.StatusCode == StatusCode.Success)
            {
                _alertModel = new()
                {
                    Status = AlertStatus.Success,
                    Message = AlertStatus.Success.ToString()
                };
            }
            else
            {
                _alertModel = new()
                {
                    Status = AlertStatus.Error,
                    Message = response.Message
                };
            }

            _initSortingAndSelect = true;
        }

        public async Task OnMarketGroupClickEvent(int id)
        {
            _selectedMarketGroupId = id;
            _marketTemplates = new();
            if (id != 1)
            {
                var marketTemplateGroup = _clientMarketTemplateGroupsRef.FirstOrDefault(x => x.Id == id);
                _marketTemplates.AddRange(_filteredMarketTemplatesRef.Where(x => marketTemplateGroup.MarketTemplateIds.Contains(x.MarketTemplateId)).ToList());
            }
            else
            {
                _marketTemplates.AddRange(_filteredMarketTemplatesRef);
            }
            _isMarketGroupClicked = true;
            StateHasChanged();
        }

        public async Task PrepareMarketSortOrderModel(MarketSortOrderFilterModel model)
        {
            _filterModel = model;

            _isMarketPreLoader = true;
            await LoadData();
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
                    foreach (var market in _marketTemplates.Where(c => c.MarketTemplateId == marketTemplateId))
                    {
                        if (!market.MarketTemplateGroupNames.Select(s => s.ToLower()).Contains(marketGroup.Name.ToLower()))
                            market.MarketTemplateGroupNames.Add(marketGroup.Name);
                    }
                }
                else
                {
                    foreach (var market in _marketTemplates.Where(c => c.MarketTemplateId == marketTemplateId))
                    {
                        if (market.MarketTemplateGroupNames.Select(s => s.ToLower()).Contains(marketGroup.Name.ToLower()))
                            market.MarketTemplateGroupNames.Remove(marketGroup.Name);
                    }
                }
            }
        }

        public async Task OnModalDiscardChanges()
        {
            _isMarketPreLoader = true;
            await InvokeAsync(StateHasChanged);

            _marketTemplates = new List<ClientMarketTemplate>();
            if (_selectedMarketGroupId != 1)
            {
                var marketTemplateGroup = _clientMarketTemplateGroupsRef.FirstOrDefault(x => x.Id == _selectedMarketGroupId);
                _marketTemplates.AddRange(_filteredMarketTemplatesRef.Where(x => marketTemplateGroup.MarketTemplateIds.Contains(x.MarketTemplateId)).ToList());
            }
            else
            {
                _marketTemplates.AddRange(_filteredMarketTemplatesRef);
            }

            _isUpdate = false;
            _isMarketPreLoader = false;
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnSaveCopyClickEvent() => _alertModel = new()
        {
            Status = AlertStatus.Success,
            Message = "Market Sort Order settings copied successfully."
        };

        public async ValueTask DisposeAsync()
        {

            if (DataReference is not null)
                await DataReference.DisposeAsync();

            DataReference = null;
        }

        public void Dispose()
        {
            //GC.SuppressFinalize(this);
        }

        #endregion


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
        private async Task LoadData()
        {
            _filteredMarketTemplatesRef = new();
            _clientMarketTemplateGroupsRef = new();
            _marketTemplates = new();

            if (!string.IsNullOrEmpty(_filterModel.SelectedClientCode) && _filterModel.SelectedClientCode != "NA" && _filterModel.SelectedSportId != 0)
            {
                var marketTemplateResponse = (await TradingService.GetClientMarketTemplates(_filterModel.SelectedClientCode, _filterModel.SelectedSportId, _filterModel.SelectedFixtureStatusId, _translationState.ActiveLanguageCode)).Result;

                var marketTemplateGroupResponse = (await TradingService.GetClientMarketTemplateGroups(_filterModel.SelectedClientCode, _filterModel.SelectedSportId, _filterModel.SelectedFixtureStatusId)).Result;

                _clientMarketTemplateGroupsRef = marketTemplateGroupResponse?.ClientMarketTemplateGroups
                    .Select(s => new ClientMarketTemplateGroup
                    {
                        Id = s.MarketTemplateGroupId,
                        Name = s.Name,
                        MarketTemplateIds = s.MarketTemplateIds.Split(",").ToList(),
                        SortOrder = s.SortOrder,
                        IsSelected = s.Name == "All Markets" ? true : false,
                    }).ToList() ?? new();

                _filteredMarketTemplatesRef = marketTemplateResponse?.MarketTemplates
                    .OrderBy(o => o.SortOrder)
                    .Select((s, index) => new ClientMarketTemplate
                    {
                        MarketTemplateGroupNames = s.MarketTemplateGroupNames,
                        MarketTemplateId = s.MarketTemplateId,
                        MarketTemplateName = s.MarketTemplateName,
                        SortOrder = s.SortOrder
                    }).ToList() ?? new List<ClientMarketTemplate>();
            }

            _selectedMarketGroupId = _clientMarketTemplateGroupsRef.FirstOrDefault(x => x.Name.ToUpper() == "ALL MARKETS")?.Id ?? -1;

            _marketGroups = _clientMarketTemplateGroupsRef?
                .Where(s => s.Id != _selectedMarketGroupId)
                .Select(s => new SelectListItemModel<int>
                {
                    Value = s.Id,
                    Name = s.Name
                }).ToList() ?? new();
            _marketTemplates.AddRange(_filteredMarketTemplatesRef);

            // set InitMultipleSelect to true to enable multiple select2.
            _initSortingAndSelect = true;
            _isMarketPreLoader = false;
        }
    }
}
