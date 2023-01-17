using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Exceptions;

namespace Backoffice.Web.Components.OddsEngine.MarketGroups
{
    public partial class CopyMarketGroupModal : IDisposable
    {
        #region Properties
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        public MarketGroupModel MarketGroupModel { get; set; }
        public List<ClientInformationModel> ClientInformations { get; set; }

        public List<SelectListItemModel<string>> Clients { get; set; }
        public string SelectedClientCode { get; set; }
        public List<SelectListItemModel<int>> Sports { get; set; }
        public int SelectedSportId { get; set; }
        public List<SelectListItemModel<int>> FixtureStatuses { get; set; }
        public int SelectedFixtureStatusId { get; set; }
        public string CopyToClientCode { get; set; }
        public string CopyToClientName { get; set; }
        public AlertModel AlertModel { get; set; }
        public bool IsPreLoader { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsPreLoader = true;

            MarketGroupModel = new MarketGroupModel();
            AlertModel = new AlertModel();
            Clients = new List<SelectListItemModel<string>>();
            Sports = new List<SelectListItemModel<int>>();
            FixtureStatuses = new List<SelectListItemModel<int>>();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    if (UserModel.ClientCodes != null)
                    {
                        ClientInformations = (await TradingService.GetClientList()).Result;

                        var roleName = UserModel.RoleName;
                        if (roleName != "Super Admin")
                        {
                            var clientCodes = UserModel.ClientCodes;
                            ClientInformations = ClientInformations.Where(c => clientCodes.Contains(c.ClientCode)).ToList();
                        }
                        Clients = ClientInformations.Where(x => x.ClientCode != CopyToClientCode).Select(s => new SelectListItemModel<string>
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
                            Name = "No Clients Available"
                        });
                    }

                    MarketGroupModel = ModalParameters.TryGet<MarketGroupModel>("MarketGroupModel");
                    CopyToClientCode = MarketGroupModel.ClientCode;
                    CopyToClientName = ClientInformations.Where(x => x.ClientCode == CopyToClientCode).FirstOrDefault().ClientName;
                    SelectedClientCode = Clients.FirstOrDefault().Value;

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

                    SelectedSportId = Sports.Any() ? Sports.FirstOrDefault().Value : 0;

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

                    SelectedFixtureStatusId = FixtureStatuses.FirstOrDefault().Value;
                    IsPreLoader = false;
                    StateHasChanged();
                }
                catch (Exception)
                {
                }
            }
        }
        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            SelectedClientCode = e.Value.ToString();
        }
        public async void OnSportChangedEvent(ChangeEventArgs e)
        {
            SelectedSportId = int.Parse(e.Value.ToString());
        }
        public async void OnFixtureStatusChangedEvent(ChangeEventArgs e)
        {
            SelectedFixtureStatusId = int.Parse(e.Value.ToString());
        }
        public async Task OnModalCancelEvent()
        {
            ModalService.Cancel();
        }
        public async void OnModalCopyEvent()
        {
            IsPreLoader = true;
            StateHasChanged();

            if (string.IsNullOrEmpty(SelectedClientCode) || SelectedSportId <= 0 || SelectedFixtureStatusId <= -1)
            {
                AlertModel.ErrorMessage = "Please select all options in order to copy Market Group.";
                AlertModel.AlertStatus = "Error!";
                AlertModel.Opacity = 1.0m;
                AlertModel.ShowErrorMessage = true;
                IsPreLoader = false;
                return;
            }

            var marketTemplateGroupResponse = (await TradingService.GetClientMarketTemplateGroups(SelectedClientCode, SelectedSportId, SelectedFixtureStatusId)).Result;

            MarketGroupModel.MarketTemplateGroups = marketTemplateGroupResponse.ClientMarketTemplateGroups
                .Select(s => new ClientMarketTemplateGroupDTO
                {
                    MarketTemplateGroupId = s.MarketTemplateGroupId,
                    Name = s.Name,
                    MarketTemplateIds = s.MarketTemplateIds,
                    SortOrder = s.SortOrder,
                    IsActive = s.IsActive,
                    FixtureStatusId = MarketGroupModel.FixtureStatusId
                }).ToList();


            var copyToClient = ClientInformations.FirstOrDefault(f => f.ClientCode == CopyToClientCode);

            var parameters = new ModalParameters();
            parameters.Add("MarketGroupModel", MarketGroupModel);
            parameters.Add("CopyToClientId", copyToClient.ClientId);
            parameters.Add("CopyToClientCode", CopyToClientCode);
            parameters.Add("CopyToClientName", CopyToClientName);
            parameters.Add("CopyToSportId", SelectedSportId);
            parameters.Add("CopyToSportName", Sports.Where(x => x.Value == SelectedSportId).FirstOrDefault().Name);
            parameters.Add("CopyToFixtureStatusId", SelectedFixtureStatusId);
            parameters.Add("CopyToFixtureStatusName", Sports.Where(x => x.Value == SelectedSportId).FirstOrDefault().Name);

            parameters.Add("DialogStyle", "max-width: 600px;");

            IsPreLoader = false;
            var options = new ModalOptions();
            ModalService.Open<CopyConfirmationMarketGroupModalBody>("Copy Market Group", parameters, options);
        }

        public void Dispose()
        {

        }
        #endregion
    }
}
