using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Backoffice.Web.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System.Diagnostics;
using static Backoffice.Shared.Models.Response.FastOddsFixturesResponse;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class Monitoring : IDisposable
    {
        #region Properties

        [Inject] public IConfiguration Configuration { get; set; }
        [Inject] public ICommonService CommonService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        [CascadingParameter(Name = "SelectedSport")] public SportClickCallbackModel SelectedSport { get; set; }


        [Parameter] public int SportId { get; set; }
        [Parameter] public int SegmentId { get; set; }
        [Parameter] public int LeagueId { get; set; }

        private string _region = "hk";
        private HubConnection _hubConnection;
        private int _sportId;
        private int _segmentId;
        private int _leagueId;
        private PageHeaderModel _pageHeaderModel;
        private List<FixtureMarketModel>? _fixtureMarkets;
        private List<BreadcrumbModel> _breadcrumbs;
        private List<string>? _slugs;

        private bool _isPreloader = true;
        private LanguageCode _languageCode;

        private bool _init = true;
        private bool _shouldRender = true;
        private List<Client> _clients = new();
        private AlertModel _alertModel = new();
        private int _colCtr = 3;
        private bool _fixtureMarketPreLoader;

        private MonitoringCallbackModel _monitoringCallbackModel = new();

        private List<string> _channels = new List<string>();
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

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
            _slugs = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "").Split("/").ToList();

            await _hubService.InitializeOeMessageHandlers(
                ProcessOddsChange,
                ProcessBetStopFixture,
                ProcessBetStopGlobal
            );

            await PageState.AuthenticateAccessEvent.InvokeAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameters();
            if (_shouldRender)
            {
                var isSportChanged = _sportId != (SelectedSport?.SportId ?? 0) || _leagueId != (SelectedSport?.LeagueId ?? 0) || _languageCode != _translationState.ActiveLanguageCode;

                if (SelectedSport != null)
                {
                    _sportId = SelectedSport.SportId;
                    _segmentId = SelectedSport.SegmentId;
                    _leagueId = SelectedSport.LeagueId;
                }

                _languageCode = _translationState.ActiveLanguageCode;

                _pageHeaderModel = new PageHeaderModel()
                {
                    Title = $"{_translationState.GetValue(SelectedSport?.SportName ?? "")} - {SelectedSport?.SegmentName ?? ""} - {SelectedSport?.LeagueName ?? ""}",
                    IconClass = $"iconfont iconfont-{Utility.GetSportIcon(SelectedSport?.SportId ?? 0)}",
                    ShowLanguageDropDown = false
                };

                if (!_init && isSportChanged && _monitoringCallbackModel != null && !string.IsNullOrEmpty(_monitoringCallbackModel.SelectedMarketTemplateId))
                {
                    await OnInitializeEvent(_monitoringCallbackModel);
                }
            }

            _init = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    //await Subscribe();

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);

                    Log.Error(ex.Message);
                    if (ex.StackTrace != null)
                        Log.Error(ex.StackTrace.ToString());
                }

            }

            _shouldRender = false;
        }
        protected override bool ShouldRender() => _shouldRender;

        private bool ValidateParameters() => _init || ((SelectedSport?.SportId ?? 0) > 0 && _slugs != null && _slugs.Count >= 2);

        private async Task OnInitializeEvent(MonitoringCallbackModel model)
        {
            _isPreloader = true;
            _shouldRender = true;

            _fixtureMarkets = await GetFixtures(new MarketFixtureRequest
            {
                FixtureTypeId = (int)FixtureType.Match,
                SportId = _sportId,
                SegmentId = _segmentId,
                LeagueId = _leagueId,
                MarketTemplateId = model.SelectedMarketTemplateId,
                SearchText = model.SearchText,
            });

            await SubscribeToChannel($"leagues:{_leagueId}:fixtures");

            _isPreloader = false;
            _shouldRender = true;
        }

        private async Task MonitoringCallback(MonitoringCallbackModel model) => await OnInitializeEvent(_monitoringCallbackModel = model);

        private Task FilterClientCallback(List<SelectListItemWithCheckboxModel<string>> clients)
        {
            if (clients != null)
            {
                _clients = clients.Where(c => c.Value != "all" && c.IsChecked).Select(s => new Client
                {
                    ClientCode = s.Value,
                    ClientName = s.Name,
                    Region = _region
                }).ToList();
                _colCtr = 3 + _clients.Count();

                if (_userState.ClaimsPrincipal?.Claims?.Any(x => x.Type == $"{SportsbookClientPermission.SBC_OE_Monitoring_MatchType.ToString().Replace("_", ".")}" && x.Value == "1") ?? false)
                    _colCtr++;

            }

            _shouldRender = true;
            return Task.CompletedTask;
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
                        Status = AlertStatus.Success,
                        Message = $"{model.FixtureName} has been successfully closed."
                    };
                }
                else
                {
                    _alertModel = new AlertModel
                    {
                        Opacity = 1m,
                        Status = AlertStatus.Error,
                        Message = $"{model.FixtureName} is NOT closed successfully."
                    };
                }

                _shouldRender = true;
            }
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
        private async Task SubscribeToChannel(string key)
        {
            if (_channels.Contains(key))
                return;

            foreach (var channel in _channels)
            {
                await _hubService.UnSubscribeToChannel(channel);
            }

            _channels.Clear();

            await _hubService.SubscribeToChannel(key);
            _channels.Add(key);
        }
        private async Task<List<FixtureMarketModel>> GetFixtures(MarketFixtureRequest request)
        {
            var response = await _backofficeService.GetMarketFixtures(request, _languageCode);
            var marketFixtures = response.Result;

            if (marketFixtures.Fixtures.Count == 0)
                return new();

            List<FixtureMarketModel> fixtureMarkets = new();
            foreach (var fixture in marketFixtures.Fixtures.Where(x => string.IsNullOrEmpty(request.SearchText) || (x.FixtureName.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase))))
            {
                var market = GetNearestLineToTwo(fixture.Markets);

                if (market == null)
                    continue;

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
                    //MarketSpecifier = market.MarketSpecifier,
                    MarketStatusId = market.MarketStatusId,
                    //MarketStatusName = market.MarketStatusName,
                    //MarketTemplateId = market.MarketTemplateId,
                    CorrelationId = Guid.NewGuid().ToString(),
                    Selections = market.Selections
                });
            }

            return fixtureMarkets;
        }
        private MarketModel? GetNearestLineToTwo(List<MarketModel> markets)
        {
            if (!markets.Any())
                return null;

            if (markets.Count == 1)
                return markets.FirstOrDefault();

            var market = markets.FirstOrDefault(s => s.MarketStatusId <= (int)MarketStatus.Active);

            if (market == null)
                return null;

            if (market.Selections.Count > 2)
                return market;

            var evenLine = 2m;

            var selections = markets.SelectMany(s => s.Selections.Select(se => new
            {
                s.MarketId,
                s.Line,
                Odds = Convert.ToDecimal(se.DecimalOdds),
                Diff = Math.Abs(evenLine - Convert.ToDecimal(se.DecimalOdds))
            }))
                            .OrderBy(o => o.Diff)
                            .ThenBy(o => Convert.ToDecimal(o.Line))
                            .Distinct()
                            .ToList();

            if (!selections.Any())
                return null;

            //var nearestSelection = selections.Aggregate((s1, s2) => Math.Abs(s1.Odds - evenLine) <= Math.Abs(s2.Odds - evenLine) ? s1 : s2);
            var nearestSelections = selections.Where(x => x.Diff == selections.FirstOrDefault()?.Diff);

            if (nearestSelections.Any() && nearestSelections.Count() > 1)
            {
                nearestSelections = nearestSelections
                    .Select(s => new
                    {
                        s.MarketId,
                        s.Line,
                        s.Odds,
                        Diff = Math.Abs(0.5m - (1 / s.Odds))
                    })
                    .OrderBy(o => o.Diff)
                    .ThenBy(o => Convert.ToDecimal(o.Line))
                    .Distinct();
            }

            var nearestSelection = nearestSelections.FirstOrDefault();

            var result = markets.FirstOrDefault(c => c.MarketId == nearestSelection?.MarketId);

            result.UnixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            result.Selections.ForEach(selection => selection.UnixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            return markets.FirstOrDefault(c => c.MarketId == nearestSelection?.MarketId);
        }

        private async Task ProcessOddsChange(OddsChangeFixtureMessage message)
        {
            Debug.WriteLine($" ==> Message Received: {message.FixtureId} - {message.FixtureName}");

            if (_fixtureMarkets == null)
                return;

            if (_sportId != message.SportId || _segmentId != message.SegmentId || _leagueId != message.LeagueId)
                return;

            if (message.FixtureTypeId != (int)FixtureType.Match)
                return;

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
                currentFixture.UnixTime = message.UnixTime;

                if (currentFixture.FixtureStatusId > (int)FixtureStatus.Live)
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
                            currentMarket.ClientCode = newMarket.ClientCode;
                            currentMarket.ClientCodeMargin = newMarket.ClientCodeMargin;
                            currentMarket.UnixTime = newMarket.UnixTime;

                            foreach (var newSelection in newMarket.Selections)
                            {
                                var providerSelectionId = $"{currentFixture.ProviderFixtureId}:{newMarket.MarketTemplateId}:{newSelection.SelectionTemplateId}:{newMarket.MarketSpecifier}";
                                var currentSelection = currentMarket.Selections.FirstOrDefault(c => c.ProviderSelectionId == providerSelectionId);

                                var clientOdds = new List<ClientOddsModel>();
                                foreach (var client in newMarket.ClientSelections)
                                {
                                    var clientSelection = client.Selections.FirstOrDefault(c => c.SelectionId == newSelection.SelectionId);
                                    clientOdds.Add(new ClientOddsModel()
                                    {
                                        ClientCode = client.ClientCode,
                                        Odds = clientSelection?.DecimalOdds ?? -99,
                                        SelectionTemplateId = newSelection.SelectionTemplateId,
                                        SelectionId = newSelection.SelectionId
                                    });
                                }

                                if (currentSelection != null)
                                {
                                    if (newMarket.ClientCodeMargins == null)
                                        return;

                                    if (newSelection.IsActive)
                                    {
                                        currentSelection.DecimalOdds = CommonService.GetFinalOdds(newSelection.DecimalOdds).ToString("0.00");
                                        currentSelection.IsActive = newSelection.IsActive;
                                        currentSelection.CorrelationId = Guid.NewGuid().ToString();
                                        currentSelection.ClientOdds = clientOdds;
                                        currentSelection.UnixTime = newSelection.UnixTime;
                                        //currentSelection.ClientOdds = currentMarket.ClientCode != null ? currentMarket.ClientCode.Split.Select(s =>
                                        //    new ClientOddsModel
                                        //    {
                                        //        ClientCode = s.ClientCode,
                                        //        SelectionTemplateId = s.SelectionTemplateId,
                                        //        Odds = s.DecimalOdds,
                                        //        OldOdds = currentSelection.ClientOdds != null && currentSelection.ClientOdds.Any(c => c.ClientCode == s.ClientCode) ?
                                        //                    currentSelection.ClientOdds?.FirstOrDefault(c => c.ClientCode == s.ClientCode)?.Odds ?? s.DecimalOdds : s.DecimalOdds
                                        //    }).ToList() : new List<ClientOddsModel>();
                                    }
                                }
                            }
                        }
                    }
                }

                _shouldRender = true;
                await InvokeAsync(StateHasChanged);
            }
            else if (message.FixtureStatusId < (int)FixtureStatus.Suspended)
            {
                var markets = message.Markets.Where(c => c.MarketTemplateId == _monitoringCallbackModel.SelectedMarketTemplateId).ToList();

                if (markets != null && markets.Any() && markets.Count > 0)
                {
                    Parallel.ForEach(markets, market =>
                    {
                        var selections = new List<SelectionModel>();

                        foreach (var newSelection in market.Selections)
                        {
                            var clientOdds = new List<ClientOddsModel>();
                            foreach (var client in market.ClientSelections)
                            {
                                var clientSelection = client.Selections.FirstOrDefault(c => c.SelectionId == newSelection.SelectionId);
                                clientOdds.Add(new ClientOddsModel()
                                {
                                    ClientCode = client.ClientCode,
                                    Odds = clientSelection?.DecimalOdds ?? -99,
                                    SelectionTemplateId = newSelection.SelectionTemplateId,
                                    SelectionId = newSelection.SelectionId
                                });
                            }

                            selections.Add(new SelectionModel()
                            {
                                SelectionId = newSelection.SelectionId.ToString(),
                                ProviderSelectionId = $"{message.ProviderFixtureId}:{market.MarketTemplateId}:{newSelection.SelectionTemplateId}:{market.MarketSpecifier}",
                                //OldDecimalOdds = CommonService.GetFinalOdds(s.DecimalOdds).ToString("0.00"),
                                DecimalOdds = CommonService.GetFinalOdds(newSelection.DecimalOdds).ToString("0.00"),
                                IsActive = newSelection.IsActive,
                                SelectionName = newSelection.SelectionName,
                                SelectionTemplateId = newSelection.SelectionTemplateId,
                                SortOrder = newSelection.SortOrder,
                                UnixTime = newSelection.UnixTime,
                                ClientOdds = clientOdds
                            });
                        }

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
                            Line = market.Line,
                            MarketName = market.MarketName,
                            MarketTemplateId = market.MarketTemplateId,
                            ProviderMarketId = $"{message.ProviderFixtureId}:{market.MarketTemplateId}:{market.MarketSpecifier}",
                            //MarketSpecifier = m.MarketSpecifier,
                            MarketStatusId = market.MarketStatusId,
                            //MarketStatusName = ((MarketStatus)m.MarketStatusId).ToString(),
                            MarketId = market.MarketId.ToString(),
                            ClientCode = market.ClientCode,
                            ClientCodeMargin = market.ClientCodeMargin,
                            CorrelationId = Guid.NewGuid().ToString(),
                            UnixTime = market.UnixTime,
                            Selections = selections
                        };

                        _fixtureMarkets.Add(fixture);
                    });

                    _fixtureMarkets = _fixtureMarkets.OrderByDescending(o => o.FixtureStatusId).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();
                }

                _shouldRender = true;
                await InvokeAsync(StateHasChanged);
            }

        }

        private async Task ProcessBetStopFixture(BetStopFixtureMessage message)
        {
            if (_fixtureMarkets != null && _fixtureMarkets.Any())
            {
                var currentFixture = _fixtureMarkets?.FirstOrDefault(c => c.ProviderFixtureId == message.ProviderFixtureId && c.MarketTemplateId == _monitoringCallbackModel.SelectedMarketTemplateId);

                if (currentFixture != null)
                {
                    var fixtureMarkets = _fixtureMarkets?.Where(c => c.ProviderFixtureId == message.ProviderFixtureId).ToList() ?? new List<FixtureMarketModel>();
                    fixtureMarkets.ForEach(c =>
                    {
                        c.BetStop = message.IsBetStop ? new List<int>() { 1 } : new List<int>();
                        c.CorrelationId = Guid.NewGuid().ToString();
                    });

                    _shouldRender = true;
                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        private async Task ProcessBetStopGlobal(BetStopGlobalMessage message)
        {
            if (_fixtureMarkets != null && _fixtureMarkets.Any())
            {
                _fixtureMarkets.ForEach(c =>
                {
                    c.IsBetStopGlobal = message.IsBetStop;
                    c.CorrelationId = Guid.NewGuid().ToString();
                });

                _shouldRender = true;
                await InvokeAsync(StateHasChanged);
            }
        }

        #endregion

    }
}
