using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.Shared;
using Backoffice.Web.Shared.OddsEngine;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;
using Serilog;
using static Backoffice.Shared.Models.Response.FastOddsFixturesResponse;

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
        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }


        [Parameter] public int SportId { get; set; }
        [Parameter] public int SegmentId { get; set; }
        [Parameter] public int LeagueId { get; set; }

        private string _region = "hk";
        private HubConnection _hubConnection;
        private int _sportId;
        private int _segmentId;
        private int _leagueId;
        private PageHeaderModel _pageHeaderModel;
        private MonitoringCallbackModel _monitoringCallbackModel;
        private List<Client>? _clients;
        private List<FixtureMarketModel>? _fixtureMarkets;
        private List<BreadcrumbModel> _breadcrumbs;
        private List<string>? _slugs;

        private bool _isPreloader;
        private AlertModel _alertModel;
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            _isPreloader = true;
            _clients = new();
            _alertModel = new();
            _fixtureMarkets = null;
            _monitoringCallbackModel = new();
            _slugs = new List<string>();
            _breadcrumbs = new()
            {
                new()
                {
                    Name = "Monitoring",
                    Url = $"{_region}/odds-engine/monitoring",
                    IsCurrentPage = true
                }
            };
            TradingService.Region = _region.ToUpper();
            _slugs = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "").Split("/").ToList();

            await AuthenticateAccess.InvokeAsync();

            var clientsResponse = await TradingService.GetClientList();
            var clients = clientsResponse.Result;

            if (clients != null)
            {
                _clients = UserClientCodes != null && UserClientCodes.Any() ?
                    clients.Where(c => UserClientCodes.Contains(c.ClientCode.ToUpper()))
                    .Select(s => new Client
                    {
                        ClientCode = s.ClientCode,
                        ClientName = s.ClientName,
                        Region = Configuration?["Region"] ?? "HK"
                    })
                    .ToList() : new();
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            if (SelectedSport != null && _slugs != null && _slugs.Count >= 2)
            {
                var isSportChanged = _sportId != SelectedSport.SportId || _leagueId != SelectedSport.LeagueId;
                _sportId = SelectedSport.SportId;
                _segmentId = SelectedSport.SegmentId;
                _leagueId = SelectedSport.LeagueId;

                _pageHeaderModel = new PageHeaderModel()
                {
                    Title = $"{SelectedSport.SportName} - {SelectedSport.SegmentName} - {SelectedSport.LeagueName}",
                    IconClass = $"iconfont iconfont-{SelectedSport.SportName.ToLower().Replace(" ", "-")}"
                };

                if (isSportChanged && _monitoringCallbackModel != null && !string.IsNullOrEmpty(_monitoringCallbackModel.SelectedMarketTemplateId))
                {
                    _fixtureMarkets = new();
                    await OnInitializeEvent(_monitoringCallbackModel);
                }
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

                    Log.Error(ex.Message);
                    if (ex.StackTrace != null)
                        Log.Error(ex.StackTrace.ToString());
                }

            }
        }

        private async Task OnInitializeEvent(MonitoringCallbackModel model)
        {
            _isPreloader = true;

            _fixtureMarkets = await GetFixtures(new MarketFixtureRequest
            {
                FixtureTypeId = (int)FixtureType.Match,
                SportId = _sportId,
                SegmentId = _segmentId,
                LeagueId = _leagueId,
                MarketTemplateId = model.SelectedMarketTemplateId,
                SearchText = model.SearchText,
            });

            _isPreloader = false;
        }

        private async Task MonitoringCallback(MonitoringCallbackModel model)
        {
            await OnInitializeEvent(_monitoringCallbackModel = model);
        }

        private async Task OnSaveClickedEvent(object param)
        {
            var model = (DeactivateFixtureCallbackModel)param;

            if (model != null)
            {
                if (model.Result)
                {
                    await OnInitializeEvent(_monitoringCallbackModel);

                    _alertModel = new AlertModel
                    {
                        Opacity = 1m,
                        Status = AlertStatus.Success.ToString(),
                        Message = $"{model.FixtureName} has been successfully closed."
                    };
                }
                else
                {
                    _alertModel = new AlertModel
                    {
                        Opacity = 1m,
                        Status = AlertStatus.Error.ToString(),
                        Message = $"{model.FixtureName} is NOT closed successfully."
                    };
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            this.Dispose();
        }

        public async void Dispose()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
        }

        #endregion

        #region Utilities
        private async Task Subscribe()
        {
            var message = string.Empty;
            var connectionId = string.Empty;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(new Uri(Path.Combine(NavigationManager.BaseUri, "bohub")))
            .Build();

            _hubConnection.On<OnExternalOddsChangeMessage>("onoddschange", async (message) =>
            {
                await ProcessOddsChange(message);
            });
            _hubConnection.On<OnExternalBetStopFixtureMessage>("onbetstopfixture", async (message) =>
            {
                await ProcessBetStopFixture(message);
            });
            _hubConnection.On<OnExternalBetStopGlobalMessage>("onbetstopglobal", async (message) =>
            {
                await ProcessBetStopGlobal(message);
            });

            _hubConnection.Reconnecting += async (ex) =>
            {
                message = ex?.Message ?? "";
            };

            await _hubConnection.StartAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    message = "Error Connecting to hub";
                }
                else if (task.IsCompletedSuccessfully)
                {
                    connectionId = _hubConnection.ConnectionId?.ToString();
                }
            });

            await _hubConnection.InvokeAsync("AddToGroup", _region.ToUpper());

        }
        private async Task<List<FixtureMarketModel>> GetFixtures(MarketFixtureRequest request)
        {
            var response = await BackOfficeService.GetMarketFixtures(request);
            var marketFixtures = response.Result;

            if (marketFixtures.Fixtures.Count == 0)
                return new();

            List<FixtureMarketModel> fixtureMarkets = new();
            foreach (var fixture in marketFixtures.Fixtures.Where(x => string.IsNullOrEmpty(request.SearchText) || (x.FixtureName.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase))))
            {
                fixtureMarkets.AddRange(fixture.Markets.Select(market => new FixtureMarketModel()
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
                    //MarketSpecifier = market.MarketSpecifier,
                    MarketStatusId = market.MarketStatusId,
                    //MarketStatusName = market.MarketStatusName,
                    //MarketTemplateId = market.MarketTemplateId,
                    Selections = market.Selections
                }));
            }

            return fixtureMarkets;
        }

        private async Task ProcessOddsChange(OnExternalOddsChangeMessage message)
        {
            if (_fixtureMarkets == null)
                return;

            if (_sportId != message.SportId || _segmentId != message.SegmentId || _leagueId != message.LeagueId)
                return;

            if (message.FixtureTypeId != (int)FixtureType.Match)
                return;

            //var emptyContentModels = new List<MarketContentModel>();

            if (!message.Markets.Any(s => s.MarketTemplateId == _monitoringCallbackModel.SelectedMarketTemplateId))
                return;

            var currentFixture = _fixtureMarkets.FirstOrDefault(c => c.ProviderFixtureId == message.ProviderFixtureId);

            if (currentFixture != null)
            {
                currentFixture.BetStop = new List<int>();
                currentFixture.CurrentPeriod = message.CurrentPeriod;
                currentFixture.CurrentMinute = message.CurrentMinute.ToString();
                currentFixture.Score = message.Score;
                currentFixture.FixtureStatusId = message.FixtureStatusId;

                if (currentFixture.FixtureStatusId >= (int)FixtureStatus.Suspended)
                {
                    _fixtureMarkets.Remove(currentFixture);
                }
                else
                {
                    foreach (var newMarket in message.Markets.Where(c => c.MarketTemplateId == _monitoringCallbackModel.SelectedMarketTemplateId))
                    {
                        var selectionIds = newMarket.Selections.Select(s => s.SelectionTemplateId).Distinct().ToList();

                        var providerMarketId = $"{currentFixture.ProviderFixtureId}:{newMarket.MarketTemplateId}:{newMarket.MarketSpecifier}";
                        var currentMarket = _fixtureMarkets.FirstOrDefault(c => c.ProviderFixtureId == message.ProviderFixtureId && c.ProviderMarketId == providerMarketId);

                        if (currentMarket != null)
                        {
                            currentMarket.MarketStatusId = newMarket.MarketStatusId;
                            //currentMarket.MarketStatusName = ((MarketStatus)newMarket.MarketStatusId).ToString();
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
                                                            currentSelection.ClientOdds?.FirstOrDefault(c => c.ClientCode == s.ClientCode)?.Odds ?? s.DecimalOdds : s.DecimalOdds
                                            }).ToList() : new List<ClientOddsModel>();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (message.FixtureStatusId < (int)FixtureStatus.Suspended)
            {
                var markets = message.Markets.Where(c => c.MarketTemplateId == _monitoringCallbackModel.SelectedMarketTemplateId).ToList();

                if (markets != null && markets.Any() && markets.Count > 0)
                {
                    Parallel.ForEach(markets, m =>
                    {
                        _fixtureMarkets.Add(new FixtureMarketModel()
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
                            //MarketSpecifier = m.MarketSpecifier,
                            MarketStatusId = m.MarketStatusId,
                            //MarketStatusName = ((MarketStatus)m.MarketStatusId).ToString(),
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
                        });
                    });
                }
            }
            await InvokeAsync(StateHasChanged);
        }

        private async Task ProcessBetStopFixture(OnExternalBetStopFixtureMessage message)
        {
            if (_fixtureMarkets != null && _fixtureMarkets.Any())
            {
                var currentFixture = _fixtureMarkets?.FirstOrDefault(c => c.ProviderFixtureId == message.ProviderFixtureId && c.MarketTemplateId == _monitoringCallbackModel.SelectedMarketTemplateId);

                if (currentFixture != null)
                {
                    var fixtureMarkets = _fixtureMarkets?.Where(c => c.ProviderFixtureId == message.ProviderFixtureId).ToList() ?? new List<FixtureMarketModel>();
                    fixtureMarkets.ForEach(x => x.BetStop = message.IsBetStop ? new List<int>() { 1 } : new List<int>());

                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        private async Task ProcessBetStopGlobal(OnExternalBetStopGlobalMessage message)
        {
            if (_fixtureMarkets != null && _fixtureMarkets.Any())
            {
                _fixtureMarkets.ForEach(c => c.IsBetStopGlobal = message.IsBetStop);
                await InvokeAsync(StateHasChanged);
            }
        }

        #endregion

    }
}
