using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Backoffice.Web.Components.OddsEngine.MarketSortOrder
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public ITradingService TradingService { get; set; }
        [Inject] private IModalService ModalService { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel? UserModel { get; set; }
        [CascadingParameter(Name = "UserClientCodes")] public List<string>? UserClientCodes { get; set; }

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
        [Parameter] public string Region { get; set; }

        public List<SelectListItemModel<string>> Clients { get; set; }
        public List<SelectListItemModel<int>> Sports { get; set; }
        public List<SelectListItemModel<int>> FixtureStatuses { get; set; }
        public List<MarketSortOrderModel.ClientMarketTemplateGroup> ClientMarketTemplateGroups { get; set; }
        private MarketSortOrderFilterModel _filterModel;
        private List<ClientInformationModel> ClientsInformation;
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
            ClientsInformation = new();

            if (UserClientCodes != null && UserClientCodes.Any())
            {
                var response = await TradingService.GetClientList();

                ClientsInformation = response.Result;
                var roleName = UserModel?.RoleName;

                if (roleName != "Super Admin")
                {
                    var clientCodes = UserClientCodes;
                    ClientsInformation = ClientsInformation.Where(c => clientCodes.Contains(c.ClientCode)).ToList();
                }

                Clients = ClientsInformation.Select(s => new SelectListItemModel<string>
                {
                    Name = s.ClientName,
                    Value = s.ClientCode
                }).OrderBy(x => x.Name).ToList();
            }
            else
            {
                Clients.Add(new SelectListItemModel<string>
                {
                    Value = "",
                    Name = "No Clients Available"
                });
            }

            var sports = (await TradingService.GetSportLookup()).Result;

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

            _filterModel = new()
            {
                ClientId = ClientsInformation?.FirstOrDefault()?.ClientId.ToString() ?? string.Empty,
                SelectedClientCode = ClientsInformation?.FirstOrDefault()?.ClientCode ?? string.Empty,
                SelectedSportId = Sports.FirstOrDefault()?.Value ?? -1,
                SelectedFixtureStatusId = FixtureStatuses.FirstOrDefault()?.Value ?? -1
            };

            await Init();
        }

        public async void OnClientChangedEvent(ChangeEventArgs e)
        {
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);

            _filterModel.SelectedClientCode = e.Value?.ToString() ?? string.Empty;
            _filterModel.ClientId = ClientsInformation.FirstOrDefault(x => x.ClientCode == e.Value.ToString())?.ClientId.ToString() ?? string.Empty;
            IsUpdate = false;

            await OnCancelCallback.InvokeAsync();
            await OnInitializeEventCallback.InvokeAsync(_filterModel);
        }

        public async void OnSportChangedEvent(ChangeEventArgs e)
        {
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);

            _filterModel.SelectedSportId = int.Parse(e.Value.ToString());
            IsUpdate = false;

            await OnCancelCallback.InvokeAsync();
            await OnInitializeEventCallback.InvokeAsync(_filterModel);
        }

        public async void OnFixtureStatusChangedEvent(ChangeEventArgs e)
        {
            IsPreLoader = true;
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
            parameters.Add("ModalDialogClass", "modal-md");

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
                var marketTemplateGroupResponse = (await TradingService.GetClientMarketTemplateGroups(_filterModel.SelectedClientCode, _filterModel.SelectedSportId, _filterModel.SelectedFixtureStatusId)).Result;
                ClientMarketTemplateGroups = marketTemplateGroupResponse.ClientMarketTemplateGroups
                    .Select(s => new MarketSortOrderModel.ClientMarketTemplateGroup
                    {
                        Id = s.MarketTemplateGroupId,
                        Name = s.Name,
                        MarketTemplateIds = s.MarketTemplateIds.Split(",").ToList(),
                        SortOrder = s.SortOrder,
                        IsSelected = s.Name == "All Markets" ? true : false,
                    }).ToList();

                await OnInitializeEventCallback.InvokeAsync(_filterModel);
            }
            IsPreLoader = false;
            await IsPreLoaderChanged.InvokeAsync(IsPreLoader);
        }

        #endregion
    }
}
