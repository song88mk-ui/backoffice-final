using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Serilog;

namespace Backoffice.Web.Components.OddsEngine.MarketGroups
{
    public partial class CopyMarketGroupModal : IDisposable
    {
        #region Properties

        [CascadingParameter] public ModalParameters ModalParameters { get; set; }

        public List<PlatformClientResponse> ClientInformations { get; set; }

        public List<SelectListItemModel<string>> Clients { get; set; }
        public string SelectedClientCode { get; set; }
        public List<SelectListItemModel<int>> Sports { get; set; }
        public int SelectedSportId { get; set; }
        public List<SelectListItemModel<int>> FixtureStatuses { get; set; }
        public int SelectedFixtureStatusId { get; set; }

        public Guid CopyToClientId { get; set; }
        public string CopyToClientCode { get; set; }
        public string CopyToClientName { get; set; }
        public AlertModel AlertModel { get; set; }
        public bool IsPreLoader { get; set; }

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsPreLoader = true;
            _tradingService.Region = ModalParameters.TryGet<string>("Region").ToUpper();
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
                    CopyToClientCode = ModalParameters.TryGet<string>("ClientCode");
                    if (_userState.Model.TradingClients != null && _userState.Model.TradingClients.Any())
                    {
                        ClientInformations = _userState.Model.TradingClients;
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
                            Name = _translationState.GetValue("No Clients Available")
                        });
                    }

                    var client = ClientInformations.Where(x => x.ClientCode == CopyToClientCode).FirstOrDefault();
                    CopyToClientId = client.ClientId;
                    CopyToClientName = client.ClientName;
                    SelectedClientCode = Clients.FirstOrDefault().Value;

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
                catch (Exception ex)
                {
                    Log.Error(ex, ex.Message);
                    if (ex.StackTrace != null)
                        Log.Error(ex.StackTrace.ToString());
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
            _modalService.Cancel();
        }

        public async void OnModalCopyEvent()
        {
            IsPreLoader = true;
            StateHasChanged();

            if (string.IsNullOrEmpty(SelectedClientCode) || SelectedSportId <= 0 || SelectedFixtureStatusId <= -1)
            {
                AlertModel.Message = "Please select all options in order to copy Market Group.";
                AlertModel.Status = AlertStatus.Error;
                AlertModel.Opacity = 1.0m;
                IsPreLoader = false;
                return;
            }

            var clientId = _userState.Model.TradingClients.FirstOrDefault(c => c.ClientCode == SelectedClientCode).ClientId;

            var marketTemplateGroupResponse = (await _backofficeService.GetClientMarketTemplateGroups(clientId, SelectedSportId, SelectedFixtureStatusId)).Result;

            var copyFromMarketGroupModel = new MarketGroupModel()
            {
                ClientId = marketTemplateGroupResponse.ClientId,
                SportId = marketTemplateGroupResponse.SportId,
                FixtureStatusId = marketTemplateGroupResponse.FixtureStatusId,
                MarketTemplateGroups = marketTemplateGroupResponse.ClientMarketTemplateGroups,
                OldMarketTemplateGroups = new List<ClientMarketTemplateGroupDTO>()
            };

            var copyToClient = ClientInformations.FirstOrDefault(f => f.ClientCode == CopyToClientCode);

            var parameters = new ModalParameters();

            parameters.Add("MarketGroupModel", copyFromMarketGroupModel);
            parameters.Add("CopyToClientId", CopyToClientId);
            parameters.Add("CopyToClientCode", CopyToClientCode);
            parameters.Add("CopyToClientName", CopyToClientName);
            parameters.Add("CopyToSportId", SelectedSportId);
            parameters.Add("CopyToSportName", Sports.Where(x => x.Value == SelectedSportId).FirstOrDefault().Name);
            parameters.Add("CopyToFixtureStatusId", SelectedFixtureStatusId);
            parameters.Add("CopyToFixtureStatusName", FixtureStatuses.Where(x => x.Value == SelectedFixtureStatusId).FirstOrDefault().Name);

            parameters.Add("DialogStyle", "max-width: 600px;");

            IsPreLoader = false;
            var options = new ModalOptions();
            _modalService.Open<CopyConfirmationMarketGroupModalBody>("Copy Market Group", parameters, options);
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
