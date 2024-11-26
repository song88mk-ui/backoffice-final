using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.OddsEngine.Monitoring;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Monitoring;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Backoffice.Web.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System.Diagnostics;
using static Backoffice.Shared.Models.Response.FastOddsFixturesResponse;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;

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
        private List<MonitoringFixtureModel>? _fixtureMarkets;
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

            await _hubService.RegisterMessageHandlers(
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
        protected override async Task OnAfterRenderAsync(bool firstRender) => _shouldRender = false;
        protected override bool ShouldRender() => _shouldRender;

        public void HandleCloseMatchClick(string fixtureName, string providerFixtureId)
        {
            ModalParameters parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-dialog-centered modal-lg");
            parameters.Add("FixtureName", fixtureName);
            parameters.Add("ProviderFixtureId", providerFixtureId);

            ModalOptions options = new ModalOptions();

            _modalService.Open<CloseMatchModal>("Close Match", parameters, options);
        }
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
        private async Task<List<MonitoringFixtureModel>> GetFixtures(MarketFixtureRequest request)
        {
            var response = await _backofficeService.GetMostEvenLineMarketFixtures(request, _languageCode);
            var marketFixtures = response.Result;

            if (marketFixtures.Fixtures.Count == 0)
                return new();

            var result = new List<MonitoringFixtureModel>();
            foreach (var fixture in marketFixtures.Fixtures.Where(x => string.IsNullOrEmpty(request.SearchText) || (x.FixtureName.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase))))
            {
                result.Add(new MonitoringFixtureModel
                {
                    FixtureName = fixture.FixtureName,
                    ProviderFixtureId = fixture.ProviderFixtureId,
                    AwayName = fixture.AwayName,
                    BetStop = fixture.BetStop,
                    CurrentMinute = fixture.CurrentMinute,
                    CurrentPeriod = fixture.CurrentPeriod,
                    FixtureStartDate = fixture.FixtureStartDate,
                    FixtureStatusId = fixture.FixtureStatusId,
                    HomeName = fixture.HomeName,
                    IsBetStopGlobal = fixture.IsBetStopGlobal,
                    Score = fixture.Score,
                    MarketTemplateGroups = fixture.MarketTemplateGroups,
                    ClientMarkets = fixture.ClientMarkets.Select(clientMarket => new MonitoringFixtureClient()
                    {
                        ClientCode = clientMarket.ClientCode,
                        Market = new MonitoringFixtureMarketModel()
                        {
                            ClientCode = clientMarket.Market.ClientCode,
                            ClientCodeMargin = clientMarket.Market.ClientCodeMargin,
                            ProviderFixtureId = clientMarket.Market.ProviderFixtureId,
                            MarketTemplateId = clientMarket.Market.MarketTemplateId,
                            MarketId = clientMarket.Market.MarketId,
                            ProviderMarketId = clientMarket.Market.ProviderMarketId,
                            MarketName = clientMarket.Market.MarketName,
                            Line = clientMarket.Market.Line,
                            MarketStatusId = clientMarket.Market.MarketStatusId,
                            MarketStatusName = clientMarket.Market.MarketStatusName,
                            MarketSpecifier = clientMarket.Market.MarketSpecifier,
                            IsBoosted = clientMarket.Market.IsBoosted,
                            Selections = clientMarket.Market.Selections.Select(selection => new MonitoringFixtureSelectionModel()
                            {
                                ProviderSelectionId = selection.ProviderSelectionId,
                                SelectionId = selection.SelectionId,
                                SelectionTemplateId = selection.SelectionTemplateId,
                                SelectionName = selection.SelectionName,
                                SelectionSpecifier = selection.SelectionSpecifier,
                                DecimalOdds = selection.DecimalOdds,
                                IsActive = selection.IsActive,
                                SortOrder = selection.SortOrder
                            }).ToList()
                        }
                    }).ToList()
                });
            }

            return result;
        }
        private async Task ProcessOddsChange(OddsChangeFixtureMessage message)
        {
            if (_fixtureMarkets == null)
                return;

            if (_sportId != message.SportId || _segmentId != message.SegmentId || _leagueId != message.LeagueId)
                return;

            if (message.FixtureTypeId != (int)FixtureType.Match)
                return;

            if (message.FixtureStatusId > (int)FixtureStatus.Live)
            {
                var fixture = _fixtureMarkets.FirstOrDefault(c => c.ProviderFixtureId == message.ProviderFixtureId);

                if (fixture != null)
                    _fixtureMarkets.Remove(fixture);
            }
            else
            {
                var isReOrder = false;
                var monitoringFixture = _fixtureMarkets.FirstOrDefault(c => c.ProviderFixtureId == message.ProviderFixtureId);

                if (monitoringFixture == null)
                {
                    isReOrder = true;
                    foreach (var newMarket in message.Markets.Where(c => c.MarketTemplateId == _monitoringCallbackModel.SelectedMarketTemplateId))
                    {
                        var selections = new List<SelectionModel>();

                        foreach (var newSelection in newMarket.Selections)
                        {
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

                            selections.Add(new SelectionModel()
                            {
                                SelectionId = newSelection.SelectionId.ToString(),
                                ProviderSelectionId = $"{message.ProviderFixtureId}:{newMarket.MarketTemplateId}:{newSelection.SelectionTemplateId}:{newMarket.MarketSpecifier}",
                                DecimalOdds = CommonService.GetFinalOdds(newSelection.DecimalOdds).ToString("0.00"),
                                IsActive = newSelection.IsActive,
                                SelectionName = newSelection.SelectionName,
                                SelectionTemplateId = newSelection.SelectionTemplateId,
                                SortOrder = newSelection.SortOrder,
                                UnixTime = newSelection.UnixTime,
                                ClientOdds = clientOdds
                            });
                        }

                        // Add Market
                        //_fixtureMarkets.Add(new FixtureMarketModelV2()
                        //{
                        //    FixtureName = message.FixtureName,
                        //    MarketId = newMarket.MarketId,
                        //    ClientCode = newMarket.ClientCode,
                        //    ClientCodeMargin = newMarket.ClientCodeMargin,
                        //    MarketName = newMarket.MarketName,
                        //    ProviderMarketId = newMarket.ProviderMarketId,
                        //    ProviderFixtureId = message.ProviderFixtureId,
                        //    AwayName = message.AwayName,
                        //    BetStop = message.BetStop ?? new List<int>(),
                        //    CurrentMinute = message.CurrentMinute.ToString(),
                        //    CurrentPeriod = message.CurrentPeriod,
                        //    FixtureStartDate = message.FixtureStartDate,
                        //    FixtureStatusId = message.FixtureStatusId,
                        //    HomeName = message.HomeName,
                        //    IsBetStopGlobal = message.IsBetStopGlobal,
                        //    Score = message.Score,
                        //    Line = newMarket.Line,
                        //    MarketStatusId = newMarket.MarketStatusId,
                        //    CorrelationId = Guid.NewGuid().ToString(),
                        //    UnixTime = newMarket.UnixTime,
                        //    Selections = selections
                        //});
                    }
                }
                else
                {
                    isReOrder = monitoringFixture.FixtureStatusId != message.FixtureStatusId && message.FixtureStatusId == (int)FixtureStatus.Live;

                    monitoringFixture.CurrentPeriod = message.CurrentPeriod;
                    monitoringFixture.CurrentMinute = message.CurrentMinute.ToString();
                    monitoringFixture.Score = message.Score;
                    monitoringFixture.FixtureStatusId = message.FixtureStatusId;
                    monitoringFixture.UnixTime = message.UnixTime;

                    //foreach (var newMarket in message.Markets.Where(c => c.MarketTemplateId == _monitoringCallbackModel.SelectedMarketTemplateId))
                    //{
                    //    var selectionIds = newMarket.Selections.Select(s => s.SelectionTemplateId).Distinct().ToList();

                    //    var providerMarketId = $"{monitoringFixture.ProviderFixtureId}:{newMarket.MarketTemplateId}:{newMarket.MarketSpecifier}";
                    //    var monitoringMarket = _fixtureMarkets.FirstOrDefault(c => c.ProviderFixtureId == message.ProviderFixtureId && c.ProviderMarketId == providerMarketId);

                    //    if (monitoringMarket == null)
                    //    {
                    //        var selections = new List<SelectionModel>();

                    //        foreach (var newSelection in newMarket.Selections)
                    //        {
                    //            var clientOdds = new List<ClientOddsModel>();
                    //            foreach (var client in newMarket.ClientSelections)
                    //            {
                    //                var clientSelection = client.Selections.FirstOrDefault(c => c.SelectionId == newSelection.SelectionId);
                    //                clientOdds.Add(new ClientOddsModel()
                    //                {
                    //                    ClientCode = client.ClientCode,
                    //                    Odds = clientSelection?.DecimalOdds ?? -99,
                    //                    SelectionTemplateId = newSelection.SelectionTemplateId,
                    //                    SelectionId = newSelection.SelectionId
                    //                });
                    //            }

                    //            selections.Add(new SelectionModel()
                    //            {
                    //                SelectionId = newSelection.SelectionId.ToString(),
                    //                ProviderSelectionId = $"{message.ProviderFixtureId}:{newMarket.MarketTemplateId}:{newSelection.SelectionTemplateId}:{newMarket.MarketSpecifier}",
                    //                DecimalOdds = CommonService.GetFinalOdds(newSelection.DecimalOdds).ToString("0.00"),
                    //                IsActive = newSelection.IsActive,
                    //                SelectionName = newSelection.SelectionName,
                    //                SelectionTemplateId = newSelection.SelectionTemplateId,
                    //                SortOrder = newSelection.SortOrder,
                    //                UnixTime = newSelection.UnixTime,
                    //                ClientOdds = clientOdds
                    //            });
                    //        }

                    //        // add market
                    //        //_fixtureMarkets.Add(new FixtureMarketModel()
                    //        //{
                    //        //    FixtureName = message.FixtureName,
                    //        //    MarketId = newMarket.MarketId,
                    //        //    ClientCode = newMarket.ClientCode,
                    //        //    ClientCodeMargin = newMarket.ClientCodeMargin,
                    //        //    MarketName = newMarket.MarketName,
                    //        //    ProviderMarketId = newMarket.ProviderMarketId,
                    //        //    ProviderFixtureId = message.ProviderFixtureId,
                    //        //    AwayName = message.AwayName,
                    //        //    BetStop = message.BetStop ?? new List<int>(),
                    //        //    CurrentMinute = message.CurrentMinute.ToString(),
                    //        //    CurrentPeriod = message.CurrentPeriod,
                    //        //    FixtureStartDate = message.FixtureStartDate,
                    //        //    FixtureStatusId = message.FixtureStatusId,
                    //        //    HomeName = message.HomeName,
                    //        //    IsBetStopGlobal = message.IsBetStopGlobal,
                    //        //    Score = message.Score,
                    //        //    Line = newMarket.Line,
                    //        //    MarketStatusId = newMarket.MarketStatusId,
                    //        //    CorrelationId = Guid.NewGuid().ToString(),
                    //        //    UnixTime = newMarket.UnixTime,
                    //        //    Selections = selections
                    //        //});
                    //    }
                    //    else if (monitoringMarket != null)
                    //    {
                    //        monitoringMarket.MarketStatusId = newMarket.MarketStatusId;
                    //        monitoringMarket.ClientCode = newMarket.ClientCode;
                    //        monitoringMarket.ClientCodeMargin = newMarket.ClientCodeMargin;
                    //        monitoringMarket.UnixTime = newMarket.UnixTime;

                    //        foreach (var newSelection in newMarket.Selections)
                    //        {
                    //            var providerSelectionId = $"{monitoringFixture.ProviderFixtureId}:{newMarket.MarketTemplateId}:{newSelection.SelectionTemplateId}:{newMarket.MarketSpecifier}";
                    //            var selection = monitoringMarket.Selections.FirstOrDefault(c => c.ProviderSelectionId == providerSelectionId);

                    //            var clientOdds = new List<ClientOddsModel>();
                    //            foreach (var client in newMarket.ClientSelections)
                    //            {
                    //                var clientSelection = client.Selections.FirstOrDefault(c => c.SelectionId == newSelection.SelectionId);
                    //                clientOdds.Add(new ClientOddsModel()
                    //                {
                    //                    ClientCode = client.ClientCode,
                    //                    Odds = clientSelection?.DecimalOdds ?? -99,
                    //                    SelectionTemplateId = newSelection.SelectionTemplateId,
                    //                    SelectionId = newSelection.SelectionId
                    //                });
                    //            }

                    //            if (selection != null)
                    //            {
                    //                if (newMarket.ClientCodeMargins == null)
                    //                    return;

                    //                if (newSelection.IsActive)
                    //                {
                    //                    selection.DecimalOdds = CommonService.GetFinalOdds(newSelection.DecimalOdds).ToString("0.00");
                    //                    selection.IsActive = newSelection.IsActive;
                    //                    selection.CorrelationId = Guid.NewGuid().ToString();
                    //                    selection.ClientOdds = clientOdds;
                    //                    selection.UnixTime = newSelection.UnixTime;
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                }

                if (isReOrder)
                    _fixtureMarkets = _fixtureMarkets.OrderByDescending(o => o.FixtureStatusId).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();
            }

            _shouldRender = true;
            await InvokeAsync(StateHasChanged);
        }

        private async Task ProcessBetStopFixture(BetStopFixtureMessage message)
        {
            if (_fixtureMarkets != null && _fixtureMarkets.Any())
            {
                var currentFixture = _fixtureMarkets.Where(c => c.ProviderFixtureId == message.ProviderFixtureId).ToList();

                if (currentFixture != null)
                {
                    currentFixture.ForEach(c =>
                    {
                        c.BetStop = message.IsBetStop ? new List<int>() { 1 } : new List<int>();
                        c.UnixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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
                    c.UnixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                });

                _shouldRender = true;
                await InvokeAsync(StateHasChanged);
            }
        }

        #endregion

    }
}
