using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MarketSortOrder
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public ITradingService TradingService { get; set; }
        [Inject] private IModalService ModalService { get; set; }

        [Parameter] public EventCallback OnUpdateCallback { get; set; }
        [Parameter] public EventCallback OnCancelCallback { get; set; }
        [Parameter] public EventCallback OnSaveCallback { get; set; }
        [Parameter] public EventCallback<MarketSortOrderModel> MarketSortOrderModelCallback { get; set; }
        [Parameter] public EventCallback<MarketSortOrderFilterModel> OnInitializeEventCallback { get; set; }
        [Parameter] public EventCallback<string> OnMarketGroupCallback { get; set; }
        [Parameter] public EventCallback<bool> IsUpdateChanged { get; set; }
        [Parameter] public bool IsUpdate { get; set; }
        [Parameter] public EventCallback<bool> IsPreLoaderChanged { get; set; }
        [Parameter] public bool IsPreLoader { get; set; }
        [Parameter] public string Region { get; set; } = "HK";

        public List<SelectListItemModel<string>> Clients { get; set; }
        public List<SelectListItemModel<int>> Sports { get; set; }
        public List<SelectListItemModel<int>> FixtureStatuses { get; set; }
        public List<MarketSortOrderModel.ClientMarketTemplateGroup> ClientMarketTemplateGroups { get; set; }
        private MarketSortOrderFilterModel _filterModel;
        //private List<ClientInformationModel> ClientsInformation;
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);

            _filterModel = new();
            Clients = new List<SelectListItemModel<string>>();
            Sports = new List<SelectListItemModel<int>>();
            FixtureStatuses = new List<SelectListItemModel<int>>();
            ClientMarketTemplateGroups = new List<MarketSortOrderModel.ClientMarketTemplateGroup>();
            TradingService.Region = Region.ToUpper();
            //ClientsInformation = new();

            //var response = await TradingService.GetClientList();

            if (_userState.Model.TradingClients != null && _userState.Model.TradingClients.Any())
            {
                Clients = _userState.Model.TradingClients.Select(s => new SelectListItemModel<string>
                {
                    Value = s.ClientCode,
                    Name = s.ClientName
                }).ToList();

            }
            else
            {
                Clients.Add(new SelectListItemModel<string>
                {
                    Value = "",
                    Name = _translationState.GetValue("No Clients Available")
                });
            }

            var sports = (await _backofficeService.GetSportLookup(_translationState.ActiveLanguageCode.ToString())).Result;

            if (sports != null)
            {
                Sports = sports.SportLookup.Select(s => new SelectListItemModel<int>
                {
                    Value = s.SportId,
                    Name = s.SportName
                }).ToList();
            }
            else
            {
                Sports.Add(new SelectListItemModel<int>
                {
                    Value = 0,
                    Name = "No Sports Available"
                });
            }

            FixtureStatuses.Add(new SelectListItemModel<int>
            {
                Value = (int)FixtureStatus.Prematch,
                Name = FixtureStatus.Prematch.ToString()
            });

            FixtureStatuses.Add(new SelectListItemModel<int>
            {
                Value = (int)FixtureStatus.Live,
                Name = FixtureStatus.Live.ToString()
            });
            if (Clients.FirstOrDefault().Value == "")
            {
                Clients = _userState.Model.TradingClients.Select(s => new SelectListItemModel<string>
                {
                    Value = s.ClientCode,
                    Name = s.ClientName
                }).ToList();
            }
            var clientId = _userState.Model.TradingClients.FirstOrDefault(c => c.ClientCode == Clients.FirstOrDefault().Value).ClientId;

            _filterModel = new()
            {
                ClientId = clientId.ToString() ?? string.Empty,
                SelectedClientCode = Clients.FirstOrDefault().Value ?? string.Empty,
                SelectedSportId = Sports.FirstOrDefault()?.Value ?? -1,
                SelectedFixtureStatusId = FixtureStatuses.FirstOrDefault()?.Value ?? -1
            };

            await Init();
        }
        public async void OnClientChangedEvent(ChangeEventArgs e)
        {
            IsPreLoader = true;
            await IsPreLoaderChanged.InvokeAsync(IsPreLoader);
            await InvokeAsync(StateHasChanged);

            var clientId = _userState.Model.TradingClients.FirstOrDefault(c => c.ClientCode == e.Value.ToString()).ClientId;

            _filterModel.SelectedClientCode = e.Value?.ToString() ?? string.Empty;
            _filterModel.ClientId = clientId.ToString() ?? string.Empty;
            IsUpdate = false;

            await OnCancelCallback.InvokeAsync();
            await OnInitializeEventCallback.InvokeAsync(_filterModel);
        }

        public async void OnSportChangedEvent(ChangeEventArgs e)
        {
            IsPreLoader = true;
            await IsPreLoaderChanged.InvokeAsync(IsPreLoader);
            await InvokeAsync(StateHasChanged);

            _filterModel.SelectedSportId = int.Parse(e.Value.ToString());
            IsUpdate = false;

            await OnCancelCallback.InvokeAsync();
            await OnInitializeEventCallback.InvokeAsync(_filterModel);
        }

        public async void OnFixtureStatusChangedEvent(ChangeEventArgs e)
        {
            IsPreLoader = true;
            await IsPreLoaderChanged.InvokeAsync(IsPreLoader);
            await InvokeAsync(StateHasChanged);
            _filterModel.SelectedFixtureStatusId = int.Parse(e.Value.ToString());
            IsUpdate = false;

            await OnCancelCallback.InvokeAsync();
            await OnInitializeEventCallback.InvokeAsync(_filterModel);
        }

        public async Task OnUpdateClickEvent()
        {
            IsUpdate = true;

            await OnUpdateCallback.InvokeAsync();
        }

        public async Task OnCopyClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");

            parameters.Add("FromClientCode", _filterModel.SelectedClientCode);

            var sport = Sports.FirstOrDefault(c => c.Value == _filterModel.SelectedSportId);

            parameters.Add("FromSportId", sport.Value);
            parameters.Add("FromSportName", sport.Name);

            var fixtureStatus = FixtureStatuses.FirstOrDefault(c => c.Value == _filterModel.SelectedFixtureStatusId);

            parameters.Add("FromFixtureStatusId", fixtureStatus.Value);
            parameters.Add("FromFixtureStatusName", fixtureStatus.Name);

            var options = new ModalOptions();

            ModalService.Open<CopyModal>($"Copy Market Sort Order", parameters, options);
        }

        public async Task OnSaveClickEvent()
        {
            IsUpdate = false;

            await OnSaveCallback.InvokeAsync();
        }

        public async Task OnCancelClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");

            var client = Clients.FirstOrDefault(c => c.Value == _filterModel.SelectedClientCode);
            var clientName = client != null ? client.Name : string.Empty;

            parameters.Add("ClientName", clientName);

            var options = new ModalOptions();
            ModalService.Open<CancelModal>($"Discard Changes?", parameters, options);
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private async Task Init()
        {
            if (!IsPreLoader)
            {
                IsPreLoader = true;
                await IsPreLoaderChanged.InvokeAsync(IsPreLoader);
                await InvokeAsync(StateHasChanged);
            }

            if (!string.IsNullOrEmpty(_filterModel.SelectedClientCode) && _filterModel.SelectedClientCode != "NA" && _filterModel.SelectedSportId != 0)
            {
                var clientId = _userState.Model.TradingClients.FirstOrDefault(c => c.ClientCode == _filterModel.SelectedClientCode).ClientId;

                var marketTemplateGroupResponse = (await _backofficeService.GetClientMarketTemplateGroups(clientId, _filterModel.SelectedSportId, _filterModel.SelectedFixtureStatusId)).Result;

                if (marketTemplateGroupResponse != null)
                {
                    ClientMarketTemplateGroups = marketTemplateGroupResponse.ClientMarketTemplateGroups
                    .Select(s => new MarketSortOrderModel.ClientMarketTemplateGroup
                    {
                        Id = s.MarketTemplateGroupId,
                        Name = s.Name,
                        MarketTemplateIds = s.MarketTemplateIds?.Split(",").ToList() ?? new List<string>(),
                        SortOrder = s.SortOrder,
                        IsSelected = s.Name == "All Markets" ? true : false,
                    }).ToList();
                }

                await OnInitializeEventCallback.InvokeAsync(_filterModel);
            }
            IsPreLoader = false;
            await IsPreLoaderChanged.InvokeAsync(IsPreLoader);
        }

        #endregion
    }
}
