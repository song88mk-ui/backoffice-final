using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MonitoringDetails : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        [Parameter] public string Id { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public MonitoringDetailsCallbackModel CallbackModel { get; set; }
        public ClientModel ClientModel { get; set; }
        public List<FixtureMarketModel> FilteredMarkets { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            CallbackModel = new MonitoringDetailsCallbackModel();
            ClientModel = new ClientModel();


            var clients = (await BackOfficeService.GetClients()).Result;

            if (clients != null)
            {
                ClientModel = new ClientModel
                {
                    Clients = UserModel.ClientCodes.Any() ?
                        clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList() : new List<ClientModel.Client>()
                };
            }


            PageTitle = "American Football - USA - NCAA, Regular Season";
            IconClass = "fa fa-trophy";
        }

        public async Task OnInitEvent()
        {
            var fixture = CallbackModel.Fixture.Fixture;
            if (fixture != null)
            {
                FilteredMarkets = GetFixtureMarkets(fixture.Markets.Where(c => c.MarketTemplateId == CallbackModel.SelectedMarketTemplateId).ToList());
            }
        }

        public async Task MonitoringDetailsCallback(MonitoringDetailsCallbackModel model)
        {
            CallbackModel = model;

            await OnInitEvent();

            PageTitle = $"{model.Fixture.Fixture.SportName} - {model.Fixture.Fixture.SegmentName} - {model.Fixture.Fixture.LeagueName}";
            IconClass = $"iconfont iconfont-{model.Fixture.Fixture.SportName.ToLower().Replace(" ", "-")}";

        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        private List<FixtureMarketModel> GetFixtureMarkets(List<MarketModel> markets)
        {
            var fixture = CallbackModel.Fixture.Fixture;

            var fixtureMarkets = new List<FixtureMarketModel>();
            foreach (var market in markets)
            {
                fixtureMarkets.Add(new FixtureMarketModel()
                {
                    FixtureName = fixture.FixtureName,
                    MarketId = market.MarketId,
                    ClientCode = market.ClientCode,
                    ClientCodeMargin = market.ClientCodeMargin,
                    MarketName = market.MarketName,
                    ProviderMarketId = market.ProviderMarketId,
                    ProviderFixtureId = market.ProviderFixtureId,
                    AwayName = fixture.AwayName,
                    BetStop = fixture.BetStop,
                    CurrentMinute = fixture.CurrentMinute,
                    CurrentPeriod = fixture.CurrentPeriod,
                    FixtureStartDate = fixture.FixtureStartDate,
                    FixtureStatusId = fixture.FixtureStatusId,
                    HomeName = fixture.HomeName,
                    IsBetStopGlobal = fixture.IsBetStopGlobal,
                    Score = fixture.Score,
                    Line = market.Line,
                    MarketSpecifier = market.MarketSpecifier,
                    MarketStatusId = market.MarketStatusId,
                    MarketStatusName = market.MarketStatusName,
                    MarketTemplateId = market.MarketTemplateId,
                    Selections = market.Selections
                });
            }
            return fixtureMarkets;
        }

        #endregion

    }
}
