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
        [CascadingParameter(Name = "NavigateSportEvent")] public EventCallback<SportClickCallbackModel> NavigateSportEvent { get; set; }
        [CascadingParameter(Name = "SelectedSport")] public SportClickCallbackModel SelectedSport { get; set; }
        [Parameter] public string SportId { get; set; }
        [Parameter] public string SegmentId { get; set; }
        [Parameter] public string LeagueId { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public MonitoringCallbackModel CallbackModel { get; set; }
        public List<FixtureMarketModel> FixtureMarkets { get; set; }
        public SportsTreeModel.SportModel DefaultSport { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            FixtureMarkets = null;
            CallbackModel = new MonitoringCallbackModel();

            DefaultSport = new SportsTreeModel.SportModel();
            
            if (!string.IsNullOrEmpty(SportId))
            {
                DefaultSport.SportId = Convert.ToInt32(SportId);

                if (!string.IsNullOrEmpty(SegmentId))
                {
                    DefaultSport.Segments = new List<SportsTreeModel.SegmentModel>();

                    var leagues = new List<SportsTreeModel.LeagueModel>();
                    leagues.Add(new SportsTreeModel.LeagueModel
                    {
                        LeagueId = string.IsNullOrEmpty(LeagueId) ? 0 : Convert.ToInt32(LeagueId)
                    });

                    DefaultSport.Segments.Add(new SportsTreeModel.SegmentModel
                    {
                        SegmentId = Convert.ToInt32(SegmentId),
                        Leagues = leagues
                    });
                }
            }

            await NavigateSportEvent.InvokeAsync(new SportClickCallbackModel
            {
                SportId = string.IsNullOrEmpty(SportId) ? 0 : Convert.ToInt32(SportId),
                SegmentId = string.IsNullOrEmpty(SegmentId) ? 0 : Convert.ToInt32(SegmentId),
                LeagueId = string.IsNullOrEmpty(LeagueId) ? 0 : Convert.ToInt32(LeagueId)
            });

            PageTitle = "American Football - USA - NCAA, Regular Season";
            IconClass = "iconfon iconfont-american-football";
        }

        public async Task OnInitEvent(MonitoringCallbackModel model)
        {
            CallbackModel = model;

            FixtureMarkets = await GetFixtures(new MarketFixtureRequest
            {
                FixtureTypeId = (int)FixtureType.Match,
                SportId = 16,
                SegmentId = 43,
                LeagueId = 27625,
                MarketTemplateId = model.SelectedMarketTemplateId,
                SearchText = model.SearchText,
            });

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
