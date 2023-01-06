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
        [CascadingParameter(Name = "SelectedSport")] public SportClickCallbackModel SelectedSport { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Parameter] public string SportId { get; set; }
        [Parameter] public string SegmentId { get; set; }
        [Parameter] public string LeagueId { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public MonitoringCallbackModel CallbackModel { get; set; }
        public List<FixtureMarketModel> FixtureMarkets { get; set; }

        #endregion

        #region Fields
        public SportClickCallbackModel defaultSport;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            FixtureMarkets = null;
            CallbackModel = new MonitoringCallbackModel();

            defaultSport = new SportClickCallbackModel();

            if (!string.IsNullOrEmpty(SportId))
            {
                defaultSport.SportId = Convert.ToInt32(SportId);
                if (!string.IsNullOrEmpty(SegmentId))
                {
                    defaultSport.SegmentId = Convert.ToInt32(SegmentId);
                    defaultSport.LeagueId = Convert.ToInt32(LeagueId);
                }
            }

            PageTitle = "American Football - USA - NCAA, Regular Season";
            IconClass = "iconfon iconfont-american-football";
        }

        protected override async Task OnParametersSetAsync()
        {
            if (SelectedSport != null && defaultSport != SelectedSport)
            {
                if (SelectedSport.SportId > 0)
                {
                    defaultSport.SportId = SelectedSport.SportId;

                    if (SelectedSport.SegmentId > 0)
                    {
                        var isNewSegmentOrLeague = defaultSport.SegmentId != SelectedSport.SegmentId || defaultSport.LeagueId != SelectedSport.LeagueId;
                        defaultSport.SegmentId = SelectedSport.SegmentId;
                        defaultSport.LeagueId = SelectedSport.LeagueId;

                        if(isNewSegmentOrLeague)
                            await OnInitEvent(CallbackModel);
                    }
                }

            }
        }

        public async Task OnInitEvent(MonitoringCallbackModel model)
        {
            CallbackModel = model;

            if(model.SelectedMarketTemplateId != null)
            {

                FixtureMarkets = await GetFixtures(new MarketFixtureRequest
                {
                    FixtureTypeId = (int)FixtureType.Match,
                    SportId = defaultSport.SportId,
                    SegmentId = defaultSport.SegmentId,
                    LeagueId = defaultSport.LeagueId,
                    MarketTemplateId = model.SelectedMarketTemplateId,
                    SearchText = model.SearchText,
                });
            }


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
