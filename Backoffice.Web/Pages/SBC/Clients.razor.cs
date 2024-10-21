using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.SBC.Clients;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace Backoffice.Web.Pages.SBC
{
    public partial class Clients : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        

        
        public MetaData MetaData { get; set; }
        public ClientListResponse ClientListModel { get; set; }
        public ClientListCallbackModel ClientListCallbackModel { get; set; }
        public List<AlertModel> Alerts { get; set; }

        public bool IsPreload { get; set; }
        public decimal Opacity { get; set; }

        private static int[] virtualSportIds = new int[] { 1001, 1002, 1003, 1005, 1021 };
        private string[] cryptoCurrencies = new string[] { Currency.BTC.ToString(), Currency.ETH.ToString() };


        #endregion
        private PageHeaderModel _pageHeaderModel;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

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

            await PageState.AuthenticateAccessEvent.InvokeAsync();
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
            var exchangeRates = (await BackofficeService.GetExchangeRates()).Result;

            if (sports != null && sports.SportLookup != null && sports.SportLookup.Any())
            {
                realSports.SportLookup = sports.SportLookup.Where(s => s.SportId < 1000).ToList();

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
            parameters.Add("ExchangeRates", exchangeRates);
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
            var exchangeRates = (await BackofficeService.GetExchangeRates()).Result;

            if (sports != null && sports.SportLookup != null && sports.SportLookup.Any())
            {
                realSports.SportLookup = sports.SportLookup.Where(s => s.SportId < 1000).ToList();

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
                betSetting.BetSettingSoccer.MaxPerTicket = betSetting.BetSettingSoccer.MaxPerTicket.HasValue ? Truncate(betSetting.CurrencyCode, betSetting.BetSettingSoccer.MaxPerTicket.Value) : 0.00m;
                betSetting.BetSettingSoccer.MinPerTicket = betSetting.BetSettingSoccer.MinPerTicket.HasValue ? Truncate(betSetting.CurrencyCode, betSetting.BetSettingSoccer.MinPerTicket.Value) : 0.00m;
                betSetting.BetSettingSoccer.MaxPerGame = betSetting.BetSettingSoccer.MaxPerGame.HasValue ? Truncate(betSetting.CurrencyCode, betSetting.BetSettingSoccer.MaxPerGame.Value) : 0.00m;
                #endregion

                #region Cricket
                betSetting.BetSettingCricket.MaxPerTicket = betSetting.BetSettingCricket.MaxPerTicket.HasValue ? Truncate(betSetting.CurrencyCode, betSetting.BetSettingCricket.MaxPerTicket.Value) : 0.00m;
                betSetting.BetSettingCricket.MinPerTicket = betSetting.BetSettingCricket.MinPerTicket.HasValue ? Truncate(betSetting.CurrencyCode, betSetting.BetSettingCricket.MinPerTicket.Value) : 0.00m;
                betSetting.BetSettingCricket.MaxPerGame = betSetting.BetSettingCricket.MaxPerGame.HasValue ? Truncate(betSetting.CurrencyCode, betSetting.BetSettingCricket.MaxPerGame.Value) : 0.00m;
                #endregion

                #region Other Sports
                betSetting.BetSettingAllSports.MaxPerTicket = betSetting.BetSettingAllSports.MaxPerTicket.HasValue ? Truncate(betSetting.CurrencyCode, betSetting.BetSettingAllSports.MaxPerTicket.Value) : 0.00m;
                betSetting.BetSettingAllSports.MinPerTicket = betSetting.BetSettingAllSports.MinPerTicket.HasValue ? Truncate(betSetting.CurrencyCode, betSetting.BetSettingAllSports.MinPerTicket.Value) : 0.00m;
                betSetting.BetSettingAllSports.MaxPerGame = betSetting.BetSettingAllSports.MaxPerGame.HasValue ? Truncate(betSetting.CurrencyCode, betSetting.BetSettingAllSports.MaxPerGame.Value) : 0.00m;
                #endregion

                #region Parlay
                betSetting.BetSettingParlay.MaxPerTicket = betSetting.BetSettingParlay.MaxPerTicket.HasValue ? Truncate(betSetting.CurrencyCode, betSetting.BetSettingParlay.MaxPerTicket.Value) : 0.00m;
                betSetting.BetSettingParlay.MinPerTicket = betSetting.BetSettingParlay.MinPerTicket.HasValue ? Truncate(betSetting.CurrencyCode, betSetting.BetSettingParlay.MinPerTicket.Value) : 0.00m;
                betSetting.BetSettingParlay.MaxPayoutPerTicket = betSetting.BetSettingParlay.MaxPayoutPerTicket.HasValue ? Truncate(betSetting.CurrencyCode, betSetting.BetSettingParlay.MaxPayoutPerTicket.Value) : 0.00m;
                betSetting.BetSettingParlay.MaxWinningsBonus = betSetting.BetSettingParlay.MaxWinningsBonus.HasValue ? Truncate(betSetting.CurrencyCode, betSetting.BetSettingParlay.MaxWinningsBonus.Value) : 0.00m;
                #endregion

                #region Priceboost
                betSetting.BetSettingPriceboost.MaxStake = betSetting.BetSettingPriceboost.MaxStake.HasValue ? Truncate(betSetting.CurrencyCode, betSetting.BetSettingPriceboost.MaxStake.Value) : 0.00m;
                #endregion

                #region BetSettingBuilder
                betSetting.BetSettingBetBuilder.MaxStake = betSetting.BetSettingBetBuilder.MaxStake.HasValue ? Truncate(betSetting.CurrencyCode, betSetting.BetSettingBetBuilder.MaxStake.Value) : 0.00m;
                betSetting.BetSettingBetBuilder.MinStake = betSetting.BetSettingBetBuilder.MinStake.HasValue ? Truncate(betSetting.CurrencyCode, betSetting.BetSettingBetBuilder.MinStake.Value) : 0.00m;
                #endregion
            }

            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-xl");
            parameters.Add("Client", client);
            parameters.Add("RealSports", realSports);
            parameters.Add("VirtualSports", virtualSports);
            parameters.Add("ExchangeRates", exchangeRates);
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


        private decimal Truncate(string currencyCode, decimal value)
        {

            int places = 2;
            var formatter = "N2";

            if (cryptoCurrencies.Contains(currencyCode))
            {
                places = 12;
                formatter = "N12";

                var result = Truncate(value, places, formatter).TrimEnd('0').TrimEnd('.');
                return Convert.ToDecimal(result.Split(".").LastOrDefault()?.Length <= 1 ? Truncate(value, 2, "N2") : result);
            }

            return Convert.ToDecimal(Truncate(value, places, formatter));
        }

        private string Truncate(decimal value, int places = 2, string formatter = "")
        {
            try
            {
                return (Math.Truncate(value * (decimal)Math.Pow(10, places)) / (decimal)Math.Pow(10, places)).ToString(formatter, CultureInfo.InvariantCulture);
            }
            catch
            {
                return "0";
            }
        }

        #endregion
    }
}
