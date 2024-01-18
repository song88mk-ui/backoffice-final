using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.SBC.Clients;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class Clients : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }

        public MetaData MetaData { get; set; }
        public ClientListResponse ClientListModel { get; set; }
        public ClientListCallbackModel ClientListCallbackModel { get; set; }
        public List<AlertModel> Alerts { get; set; }

        public bool IsPreload { get; set; }
        public decimal Opacity { get; set; }

        #endregion
        private PageHeaderModel _pageHeaderModel;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _pageHeaderModel = new()
            {
                Title = "Manage Clients",
                IconClass = "ico ico-clients"
            };
            Opacity = 0m;
            Alerts = new List<AlertModel>();
            ClientListModel = new ClientListResponse();
            ClientListCallbackModel = new ClientListCallbackModel();

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 1,
            };

            await AuthenticateAccess.InvokeAsync();
        }

        public async Task OnInitEvent(ClientListCallbackModel request)
        {
            ClientListCallbackModel = request;
            ClientListModel = await GetClientList(ClientListCallbackModel.SearchText, ClientListCallbackModel.SortBy, 1);
        }

        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;
            ClientListModel = await GetClientList(ClientListCallbackModel.SearchText, ClientListCallbackModel.SortBy, MetaData.CurrentPage);
        }

        public async Task OnCreateClientClickEvent()
        {
            Alerts.Clear();
            var sports = (await BackofficeService.GetSportsAsync()).Result;
            var realSports = new SportsLookupModel();
            var virtualSports = new SportsLookupModel();

            if (sports != null && sports.SportLookup != null && sports.SportLookup.Any())
            {
                realSports.SportLookup = sports.SportLookup.Where(s => s.SportId < 1000).ToList();

                var virtualSportIds = new int[] { 1001, 1002, 1003, 1005, 1021 };

                virtualSports.SportLookup = sports.SportLookup.Where(s => virtualSportIds.Contains(s.SportId + 1000)).Select(s => new Models.SportLookup
                {
                    SportId = s.SportId + 1000,
                    SportName = $"Virtual {s.SportName}"
                }).ToList();

                virtualSports.SportLookup.Add(new Models.SportLookup
                {
                    SportId = 1055,
                    SportName = "Virtual Horse Racing"
                });

                virtualSports.SportLookup.Add(new Models.SportLookup
                {
                    SportId = 1058,
                    SportName = "Virtual Dog Racing"
                });
            }

            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-xl");
            parameters.Add("Client", new ClientListResponse.Client());
            parameters.Add("RealSports", realSports);
            parameters.Add("VirtualSports", virtualSports);
            parameters.Add("IsEdit", false);

            var options = new ModalOptions();
            ModalService.Open<CreateOrUpdateModal>("Create Client", parameters, options);
        }

        public async Task OnEditClientClickEvent(ClientListResponse.Client client)
        {
            Alerts.Clear();
            var sports = (await BackofficeService.GetSportsAsync()).Result;
            var realSports = new SportsLookupModel();
            var virtualSports = new SportsLookupModel();

            if (sports != null && sports.SportLookup != null && sports.SportLookup.Any())
            {
                realSports.SportLookup = sports.SportLookup.Where(s => s.SportId < 1000).ToList();

                var virtualSportIds = new int[] { 1001, 1002, 1003, 1005, 1021 };

                virtualSports.SportLookup = sports.SportLookup.Where(s => virtualSportIds.Contains(s.SportId + 1000)).Select(s => new Models.SportLookup
                {
                    SportId = s.SportId + 1000,
                    SportName = $"Virtual {s.SportName}"
                }).ToList();

                virtualSports.SportLookup.Add(new Models.SportLookup
                {
                    SportId = 1055,
                    SportName = "Virtual Horse Racing"
                });

                virtualSports.SportLookup.Add(new Models.SportLookup
                {
                    SportId = 1058,
                    SportName = "Virtual Dog Racing"
                });
            }

            client.BetSettings = (await BackofficeService.GetBetSetting(new BetSettingRequest
            {
                ClientId = client.ClientId
            })).Result.BetSettings ?? new List<ClientListResponse.BetSetting>();

            //transform data
            foreach (var betSetting in client.BetSettings)
            {
                #region Soccer
                betSetting.BetSettingSoccer.MaxPerTicket = betSetting.BetSettingSoccer.MaxPerTicket.HasValue ? decimal.Parse(betSetting.BetSettingSoccer.MaxPerTicket.Value.ToString("#.00")) : 0.00m;
                betSetting.BetSettingSoccer.MinPerTicket = betSetting.BetSettingSoccer.MinPerTicket.HasValue ? decimal.Parse(betSetting.BetSettingSoccer.MinPerTicket.Value.ToString("#.00")) : 0.00m;
                betSetting.BetSettingSoccer.MaxPerGame = betSetting.BetSettingSoccer.MaxPerGame.HasValue ? decimal.Parse(betSetting.BetSettingSoccer.MaxPerGame.Value.ToString("#.00")) : 0.00m;
                #endregion

                #region Cricket
                betSetting.BetSettingCricket.MaxPerTicket = betSetting.BetSettingCricket.MaxPerTicket.HasValue ? decimal.Parse(betSetting.BetSettingCricket.MaxPerTicket.Value.ToString("#.00")) : 0.00m;
                betSetting.BetSettingCricket.MinPerTicket = betSetting.BetSettingCricket.MinPerTicket.HasValue ? decimal.Parse(betSetting.BetSettingCricket.MinPerTicket.Value.ToString("#.00")) : 0.00m;
                betSetting.BetSettingCricket.MaxPerGame = betSetting.BetSettingCricket.MaxPerGame.HasValue ? decimal.Parse(betSetting.BetSettingCricket.MaxPerGame.Value.ToString("#.00")) : 0.00m;
                #endregion

                #region Other Sports
                betSetting.BetSettingAllSports.MaxPerTicket = betSetting.BetSettingAllSports.MaxPerTicket.HasValue ? decimal.Parse(betSetting.BetSettingAllSports.MaxPerTicket.Value.ToString("#.00")) : 0.00m;
                betSetting.BetSettingAllSports.MinPerTicket = betSetting.BetSettingAllSports.MinPerTicket.HasValue ? decimal.Parse(betSetting.BetSettingAllSports.MinPerTicket.Value.ToString("#.00")) : 0.00m;
                betSetting.BetSettingAllSports.MaxPerGame = betSetting.BetSettingAllSports.MaxPerGame.HasValue ? decimal.Parse(betSetting.BetSettingAllSports.MaxPerGame.Value.ToString("#.00")) : 0.00m;
                #endregion

                #region Parlay
                betSetting.BetSettingParlay.MaxPerTicket = betSetting.BetSettingParlay.MaxPerTicket.HasValue ? decimal.Parse(betSetting.BetSettingParlay.MaxPerTicket.Value.ToString("#.00")) : 0.00m;
                betSetting.BetSettingParlay.MinPerTicket = betSetting.BetSettingParlay.MinPerTicket.HasValue ? decimal.Parse(betSetting.BetSettingParlay.MinPerTicket.Value.ToString("#.00")) : 0.00m;
                betSetting.BetSettingParlay.MaxPayoutPerTicket = betSetting.BetSettingParlay.MaxPayoutPerTicket.HasValue ? decimal.Parse(betSetting.BetSettingParlay.MaxPayoutPerTicket.Value.ToString("#.00")) : 0.00m;
                betSetting.BetSettingParlay.MaxWinningsBonus = betSetting.BetSettingParlay.MaxWinningsBonus.HasValue ? decimal.Parse(betSetting.BetSettingParlay.MaxWinningsBonus.Value.ToString("#.00")) : 0.00m;
                #endregion

                #region Priceboost
                betSetting.BetSettingPriceboost.MaxStake = betSetting.BetSettingPriceboost.MaxStake.HasValue ? decimal.Parse(betSetting.BetSettingPriceboost.MaxStake.Value.ToString("#.00")) : 0.00m;
                #endregion

                #region BetSettingBuilder
                betSetting.BetSettingBetBuilder.MaxStake = betSetting.BetSettingBetBuilder.MaxStake.HasValue ? decimal.Parse(betSetting.BetSettingBetBuilder.MaxStake.Value.ToString("#.00")) : 0.00m;
                betSetting.BetSettingBetBuilder.MinStake = betSetting.BetSettingBetBuilder.MinStake.HasValue ? decimal.Parse(betSetting.BetSettingBetBuilder.MinStake.Value.ToString("#.00")) : 0.00m;
                #endregion
            }

            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-xl");
            parameters.Add("Client", client);
            parameters.Add("RealSports", realSports);
            parameters.Add("VirtualSports", virtualSports);
            parameters.Add("IsEdit", true);

            var options = new ModalOptions();
            ModalService.Open<CreateOrUpdateModal>("Edit Client", parameters, options);
        }

        public async Task OnViewBetSettingClickEvent(ClientListResponse.Client client)
        {

            client.BetSettings = (await BackofficeService.GetBetSetting(new BetSettingRequest
            {
                ClientId = client.ClientId
            })).Result.BetSettings ?? new List<ClientListResponse.BetSetting>();

            var parameters = new ModalParameters();

            parameters.Add("DialogStyle", "max-width: 30rem;");
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("BetSettings", client.BetSettings);

            var options = new ModalOptions();
            ModalService.Open<ViewBetSettingsModal>("Bet Settings", parameters, options);
        }

        public async Task OnSaveClickEvent(object item)
        {
            Opacity = Opacity == 1m ? 0.99m : 1m;

            Alerts.Clear();
            Alerts.Add(new AlertModel
            {
                Status = AlertStatus.Success,
                Message = (bool)item ? "Client successfully updated" : "Client successfully created",
                Opacity = Opacity
            });

            ClientListModel = await GetClientList(ClientListCallbackModel.SearchText, ClientListCallbackModel.SortBy, MetaData.CurrentPage);
        }

        public void Dispose()
        {
            ClientListModel = null;
            Alerts = null;

            BackofficeService.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        private async Task<ClientListResponse> GetClientList(string search, int sortBy, int page)
        {
            var request = new GetClientListRequest
            {
                Search = search,
                Page = page,
                PageSize = 25,
                SortBy = (ClientSorting)sortBy
            };

            var response = await BackofficeService.GetClientList(request);

            if (response.StatusCode == StatusCode.Success)
            {
                MetaData = new MetaData
                {
                    CurrentPage = response.Result.Page,
                    PageSize = response.Result.PageSize,
                    TotalCount = response.Result.Total,
                    TotalPages = response.Result.TotalPage,
                };

                return new ClientListResponse
                {
                    Page = response.Result.Page,
                    PageSize = response.Result.PageSize,
                    Total = response.Result.Total,
                    TotalPage = response.Result.TotalPage,
                    Clients = response.Result.Clients.Select((s, index) => new ClientListResponse.Client
                    {
                        Num = index + ((MetaData.CurrentPage - 1) * MetaData.PageSize) + 1,
                        ClientId = s.ClientId,
                        Name = s.Name,
                        StatusId = s.StatusId,
                        StatusName = s.StatusName,
                        Region = s.Region,
                        Website = s.Website,
                        Email = s.Email,
                        BetSettings = s.BetSettings,
                        BetSlipMaxSelection = s.BetSlipMaxSelection,
                        ClientCode = s.ClientCode,
                        IsEmailNotification = s.IsEmailNotification,
                        IsTierRevenue = s.IsTierRevenue,
                        MonthlyFee = s.MonthlyFee,
                        RevenueShare = s.RevenueShare,
                        TaxConfig = s.TaxConfig,
                        TelephoneNumber = s.TelephoneNumber,
                        TierJackpotShare = s.TierJackpotShare,
                        TierSportsbookShare = s.TierSportsbookShare,
                    }).ToList()
                };
            }

            return new ClientListResponse();
        }

        #endregion
    }
}
