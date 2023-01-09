using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class Monitoring : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [CascadingParameter(Name = "SelectedSport")] public SportClickCallbackModel SelectedSport { get; set; }

        [Parameter] public string SportId { get; set; }
        [Parameter] public string SegmentId { get; set; }
        [Parameter] public string LeagueId { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        public MonitoringCallbackModel MonitoringCallbackModel { get; set; }
        public ClientModel ClientModel { get; set; }
        public List<FixtureMarketModel> FixtureMarkets { get; set; }

        #endregion

        #region Fields

        public SportClickCallbackModel selectedSport = new SportClickCallbackModel();

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            FixtureMarkets = null;
            MonitoringCallbackModel = new MonitoringCallbackModel();
            ClientModel = new ClientModel();

            //selectedSport = new SportClickCallbackModel();

            //// Check url if it contains the ids for the ff: Sport ID, Segment ID, League ID
            //if (!string.IsNullOrEmpty(SportId))
            //{
            //    selectedSport.SportId = Convert.ToInt32(SportId);

            //    if (!string.IsNullOrEmpty(SegmentId))
            //    {
            //        selectedSport.SegmentId = Convert.ToInt32(SegmentId);
            //        selectedSport.LeagueId = Convert.ToInt32(LeagueId);
            //    }
            //}

            var clients = (await BackOfficeService.GetClients()).Result;

            if (clients != null)
            {
                ClientModel = new ClientModel
                {
                    Clients = UserModel.ClientCodes.Any() ?
                        clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList() : new List<ClientModel.Client>()
                };
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            if (SelectedSport != null && selectedSport != SelectedSport)
            {
                if (selectedSport.SportId != SelectedSport.SportId)
                {
                    selectedSport.SportId = SelectedSport.SportId;
                    selectedSport.SportName = SelectedSport.SportName;
                }

                if (selectedSport.SegmentId != SelectedSport.SegmentId)
                {
                    selectedSport.SegmentId = SelectedSport.SegmentId;
                    selectedSport.SegmentName = SelectedSport.SegmentName;
                }

                if (selectedSport.LeagueId != SelectedSport.LeagueId)
                {
                    selectedSport.LeagueId = SelectedSport.LeagueId;
                    selectedSport.LeagueName = SelectedSport.LeagueName;
                }

                PageTitle = $"{SelectedSport.SportName} - {SelectedSport.SegmentName} - {selectedSport.LeagueName}";
                IconClass = $"iconfont iconfont-{SelectedSport.SportName.ToLower().Replace(" ", "-")}";

                await OnInitEvent();
            }
        }

        public async Task OnInitEvent()
        {
            // if user selected a new sport OnInitEvent will be triggered by MonitoringCallback
            if (SelectedSport.SportId != MonitoringCallbackModel.SportId)
            {
                FixtureMarkets = new List<FixtureMarketModel>();
                return;
            }

            FixtureMarkets = await GetFixtures(new MarketFixtureRequest
            {
                FixtureTypeId = (int)FixtureType.Match,
                SportId = SelectedSport.SportId,
                SegmentId = SelectedSport.SegmentId,
                LeagueId = SelectedSport.LeagueId,
                MarketTemplateId = MonitoringCallbackModel.SelectedMarketTemplateId,
                SearchText = MonitoringCallbackModel.SearchText,
            });
        }

        public async Task MonitoringCallback(MonitoringCallbackModel model)
        {
            MonitoringCallbackModel = model;
            await OnInitEvent();
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        private async Task<List<FixtureMarketModel>> GetFixtures(MarketFixtureRequest request)
        {
            var marketFixtures = (await BackOfficeService.GetMarketFixtures(request)).Result;

            if (marketFixtures.Fixtures.Count == 0)
                return new List<FixtureMarketModel>();

            var fixtureMarkets = new List<FixtureMarketModel>();
            foreach (var fixture in marketFixtures.Fixtures)
            {
                foreach (var market in fixture.Markets)
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
            }

            //FixturesContentModel = new List<FixtureMarketModel>();
            //if (fixtureMarkets.Any())
            //    FixturesContentModel.AddRange(fixtureMarkets);

            return fixtureMarkets;
        }
        #endregion

    }
}
