using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class Monitoring : IAsyncDisposable, IDisposable
    {
        #region Properties

        [Inject] public IConfiguration Configuration { get; set; }
        [Inject] public ICommonService CommonService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [CascadingParameter(Name = "SelectedSport")] public SportClickCallbackModel SelectedSport { get; set; }

        [Parameter] public string SportId { get; set; }
        [Parameter] public string SegmentId { get; set; }
        [Parameter] public string LeagueId { get; set; }

        private HubConnection HubConnection { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        public MonitoringCallbackModel MonitoringCallbackModel { get; set; }
        public ClientModel ClientModel { get; set; }
        public List<FixtureMarketModel> FixtureMarkets { get; set; }
        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public List<string> Slugs { get; set; }

        public bool IsPreloader { get; set; }
        public bool PassParametersToModel { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            FixtureMarkets = null;
            MonitoringCallbackModel = new MonitoringCallbackModel();
            ClientModel = new ClientModel();
            Slugs = new List<string>();

            Breadcrumbs = new List<BreadcrumbModel>();

            Breadcrumbs.Add(new BreadcrumbModel
            {
                Name = "Monitoring",
                Url = "odds-engine/monitoring",
                IsCurrentPage = true
            });

            var clients = (await TradingService.GetClientList()).Result;

            if (clients != null)
            {
                ClientModel = new ClientModel
                {
                    Clients = UserModel.ClientCodes.Any() ?
                        clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper()))
                        .Select(s => new ClientModel.Client
                        {
                            ClientCode = s.ClientCode,
                            ClientName = s.ClientName,
                            Region = Configuration["Region"]
                        })
                        .ToList() : new List<ClientModel.Client>()
                };
            }

            Slugs = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "").Split("/").ToList();

            if (Slugs.Count > 2)
                PassParametersToModel = true;

            IsPreloader = true;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (SelectedSport != null && PassParametersToModel)
            {
                // Check if endpoint contains parameters. If yes, then pass them to the SelectedSport model
                if (Slugs.Count <= 2)
                    return;

                // If SportId, SegmentId, and LeagueId has value then monitoring will initialize data.
                if (!string.IsNullOrEmpty(SportId) && !string.IsNullOrEmpty(SegmentId) && !string.IsNullOrEmpty(LeagueId))
                {
                    if (SelectedSport.SportId != int.Parse(SportId) && SelectedSport.SegmentId != int.Parse(SegmentId) && SelectedSport.LeagueId != int.Parse(LeagueId))
                    {
                        SelectedSport.SportId = int.Parse(SportId);
                        SelectedSport.SegmentId = int.Parse(SegmentId);
                        SelectedSport.LeagueId = int.Parse(LeagueId);
                    }

                    PassParametersToModel = false;
                }
                //else if (!string.IsNullOrEmpty(SportId) && !string.IsNullOrEmpty(SegmentId))
                //{
                //    if (SelectedSport.SportId != int.Parse(SportId) && SelectedSport.SegmentId != int.Parse(SegmentId))
                //    {
                //        SelectedSport.SportId = int.Parse(SportId);
                //        SelectedSport.SegmentId = int.Parse(SegmentId);
                //    }

                //    PassParametersToModel = false;
                //}
                //else if (!string.IsNullOrEmpty(SportId))
                //{
                //    if (SelectedSport.SportId != int.Parse(SportId))
                //    {
                //        SelectedSport.SportId = int.Parse(SportId);
                //    }

                //    PassParametersToModel = false;
                //}
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    await Subscribe();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
        }

        public async Task OnInitEvent()
        {
            PageTitle = $"{SelectedSport.SportName} - {SelectedSport.SegmentName} - {SelectedSport.LeagueName}";
            IconClass = $"iconfont iconfont-{SelectedSport.SportName.ToLower().Replace(" ", "-")}";

            IsPreloader = true;

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

            IsPreloader = false;
        }

        public async Task MonitoringCallback(MonitoringCallbackModel model)
        {
            MonitoringCallbackModel = model;
            await OnInitEvent();
        }

        public async ValueTask DisposeAsync()
        {
            if (HubConnection != null)
            {
                await HubConnection.StopAsync();
                await HubConnection.DisposeAsync();
            }
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        private async Task Subscribe()
        {
            HubConnection = new HubConnectionBuilder()
            .WithUrl(new Uri(Configuration["API:HubAPI"]))
            .Build();

            HubConnection.On<OnExternalOddsChangeMessage>("onoddschange", async (message) =>
            {
                await ProcessOddsChange(message);

            });

            HubConnection.On<OnExternalBetStopFixtureMessage>("onbetstopfixture", async (message) =>
            {
                await ProcessBetStopFixture(message);
            });

            HubConnection.On<OnExternalBetStopGlobalMessage>("onbetstopglobal", async (message) =>
            {
                await ProcessBetStopGlobal(message);
            });

            await HubConnection.StartAsync();

        }
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

            return fixtureMarkets;
        }

        private async Task ProcessOddsChange(OnExternalOddsChangeMessage message)
        {
            if (FixtureMarkets == null )
                return;

            if (SelectedSport.SportId != message.SportId || SelectedSport.SegmentId != message.SegmentId || SelectedSport.LeagueId != message.LeagueId)
                return;

            if (message.FixtureTypeId != (int)FixtureType.Match)
                return;

            //var emptyContentModels = new List<MarketContentModel>();
            var newMarketTemplateIds = message.Markets.Select(s => s.MarketTemplateId).ToList();

            if (!newMarketTemplateIds.Contains(MonitoringCallbackModel.SelectedMarketTemplateId))
                return;

            var currentFixture = FixtureMarkets.FirstOrDefault(c => c.ProviderFixtureId == message.ProviderFixtureId);
            //var fixtureMarkets = Monitoring.FixtureMarkets.Where(c => c.ProviderFixtureId == message.ProviderFixtureId).ToList();

            if (currentFixture != null)
            {
                currentFixture.BetStop = new List<int>();
                currentFixture.CurrentPeriod = message.CurrentPeriod;
                currentFixture.CurrentMinute = message.CurrentMinute.ToString();
                currentFixture.Score = message.Score;
                currentFixture.FixtureStatusId = message.FixtureStatusId;

                if (currentFixture.FixtureStatusId >= (int)FixtureStatus.Suspended)
                {
                    // remove fixture if match has already ended.
                    var isRemoved = FixtureMarkets.Remove(currentFixture);

                    if (isRemoved)
                        await InvokeAsync(StateHasChanged);

                }
                else
                {
                    foreach (var newMarket in message.Markets.Where(c => c.MarketTemplateId == MonitoringCallbackModel.SelectedMarketTemplateId))
                    {
                        var selectionIds = newMarket.Selections.Select(s => s.SelectionTemplateId).Distinct().ToList();

                        var providerMarketId = $"{currentFixture.ProviderFixtureId}:{newMarket.MarketTemplateId}:{newMarket.MarketSpecifier}";
                        var currentMarket = FixtureMarkets.FirstOrDefault(c => c.ProviderFixtureId == message.ProviderFixtureId && c.ProviderMarketId == providerMarketId);

                        if (currentMarket != null)
                        {
                            currentMarket.MarketStatusId = newMarket.MarketStatusId;
                            currentMarket.MarketStatusName = ((MarketStatus)newMarket.MarketStatusId).ToString();
                            currentMarket.ClientCode = newMarket.ClientCodes;
                            currentMarket.ClientCodeMargin = newMarket.ClientCodeMargin;

                            foreach (var newSelection in newMarket.Selections)
                            {
                                var providerSelectionId = $"{currentFixture.ProviderFixtureId}:{newMarket.MarketTemplateId}:{newSelection.SelectionTemplateId}:{newMarket.MarketSpecifier}";
                                var currentSelection = currentMarket.Selections.FirstOrDefault(c => c.ProviderSelectionId == providerSelectionId);

                                if (currentSelection != null)
                                {
                                    if (newSelection.IsActive)
                                    {
                                        currentSelection.OldDecimalOdds = currentSelection.DecimalOdds;
                                        currentSelection.DecimalOdds = CommonService.GetFinalOdds(newSelection.Odds).ToString("0.00");
                                        currentSelection.IsActive = newSelection.IsActive;
                                        currentSelection.ClientOdds = newSelection.ClientOdds != null ? newSelection.ClientOdds.Select(s =>
                                            new ClientOddsModel
                                            {
                                                ClientCode = s.ClientCode,
                                                SelectionTemplateId = s.SelectionTemplateId,
                                                Odds = s.DecimalOdds,
                                                OldOdds = currentSelection.ClientOdds != null && currentSelection.ClientOdds.Any(c => c.ClientCode == s.ClientCode) ?
                                                            currentSelection.ClientOdds.FirstOrDefault(c => c.ClientCode == s.ClientCode).Odds : s.DecimalOdds
                                            }).ToList() : new List<ClientOddsModel>();
                                    }
                                    //else
                                    //{
                                    //    // remove selections
                                    //    currentMarket.Selections.Remove(currentSelection);
                                    //}
                                }
                            }
                            await InvokeAsync(StateHasChanged);
                            // remove markets
                            //if (!currentMarket.Selections.Any())
                            //    currentFixture.Markets.Remove(currentMarket);
                        }
                    }

                    // remove fixture
                    //if (!currentFixture.Markets.Any())
                    //    Monitoring.Fixtures.Remove(currentFixture);
                }

            }
            else if (message.FixtureStatusId < (int)FixtureStatus.Suspended)
            {
                var markets = message.Markets.Where(c => c.MarketTemplateId == MonitoringCallbackModel.SelectedMarketTemplateId).ToList();

                if (markets.Count > 0)
                {
                    Parallel.ForEach(markets, m =>
                    {
                        // add fixture and market
                        var fixture = new FixtureMarketModel()
                        {
                            ProviderFixtureId = message.ProviderFixtureId,
                            FixtureName = message.FixtureName,
                            HomeName = CommonService.GetHomeName(message.FixtureName, (FixtureType)message.FixtureTypeId),
                            AwayName = CommonService.GetAwayName(message.FixtureName, (FixtureType)message.FixtureTypeId),
                            CurrentPeriod = message.CurrentPeriod,
                            CurrentMinute = message.CurrentMinute.ToString(),
                            FixtureStatusId = message.FixtureStatusId,
                            FixtureStartDate = message.FixtureStartDate,
                            BetStop = new List<int>(),
                            Score = message.Score,
                            Line = m.Line,
                            MarketName = m.MarketName,
                            MarketTemplateId = m.MarketTemplateId,
                            ProviderMarketId = $"{message.ProviderFixtureId}:{m.MarketTemplateId}:{m.MarketSpecifier}",
                            MarketSpecifier = m.MarketSpecifier,
                            MarketStatusId = m.MarketStatusId,
                            MarketStatusName = ((MarketStatus)m.MarketStatusId).ToString(),
                            MarketId = m.MarketId.ToString(),
                            ClientCode = m.ClientCodes,
                            ClientCodeMargin = m.ClientCodeMargin,
                            Selections = m.Selections.Select(ss => new SelectionModel()
                            {
                                DecimalOdds = ss.Odds.ToString(),
                                IsActive = ss.IsActive,
                                SelectionName = ss.SelectionName,
                                SelectionTemplateId = ss.SelectionTemplateId,
                                ProviderSelectionId = $"{message.ProviderFixtureId}:{m.MarketTemplateId}:{ss.SelectionTemplateId}:{m.MarketSpecifier}",
                                OldDecimalOdds = ss.Odds.ToString(),
                                ClientOdds = ss.ClientOdds.Select(s =>
                                    new ClientOddsModel
                                    {
                                        ClientCode = s.ClientCode,
                                        SelectionTemplateId = s.SelectionTemplateId,
                                        Odds = s.DecimalOdds,
                                        OldOdds = 0m
                                    }).ToList()
                            }).ToList()
                        };

                        FixtureMarkets.Add(fixture);
                    });

                    //ReloadMatchComponent();
                    await InvokeAsync(StateHasChanged);

                }
            }
        }

        private async Task ProcessBetStopFixture(OnExternalBetStopFixtureMessage message)
        {
            if (FixtureMarkets != null && FixtureMarkets.Any())
            {
                var currentFixture = FixtureMarkets?.FirstOrDefault(c => c.ProviderFixtureId == message.ProviderFixtureId && c.MarketTemplateId == MonitoringCallbackModel.SelectedMarketTemplateId);

                if (currentFixture != null)
                {
                    var fixtureMarkets = FixtureMarkets.Where(c => c.ProviderFixtureId == message.ProviderFixtureId).ToList();
                    Parallel.ForEach(fixtureMarkets, f => { f.BetStop = message.IsBetStop ? new List<int>() { 1 } : new List<int>(); });

                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        private async Task ProcessBetStopGlobal(OnExternalBetStopGlobalMessage message)
        {
            if (FixtureMarkets != null && FixtureMarkets.Any())
            {
                FixtureMarkets.ForEach(c =>
                {
                    c.IsBetStopGlobal = message.IsBetStop;
                });
                await InvokeAsync(StateHasChanged);
            }
        }

        #endregion

    }
}
