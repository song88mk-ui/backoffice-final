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
using static Backoffice.Shared.Models.Response.FastOddsFixturesResponse;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
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
                ProcessBetStopFixture,
                ProcessBetStopGlobal,
                processOddsChangeMonitoring: ProcessOddsChange
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
            _fixtureMarkets = new List<MonitoringFixtureModel>();

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

            if (marketFixtures == null || marketFixtures.Fixtures.Count == 0)
                return new();

            var result = new List<MonitoringFixtureModel>();
            foreach (var fixture in marketFixtures.Fixtures.Where(x => string.IsNullOrEmpty(request.SearchText) || (x.FixtureName.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase))))
            {
                result.Add(new MonitoringFixtureModel
                {
                    FixtureId = fixture.FixtureId,
                    FixtureName = fixture.FixtureName,
                    ProviderFixtureId = fixture.ProviderFixtureId,
                    AwayName = fixture.AwayName,
                    BetStop = fixture.BetStop,
                    CurrentMinute = fixture.CurrentMinute,
                    CurrentPeriod = fixture.CurrentPeriod,
                    FixtureStartDate = fixture.FixtureStartDate,
                    FixtureStatusId = fixture.FixtureStatusId,
                    FixtureStatusName = fixture.FixtureStatusName,
                    SportId = fixture.SportId,
                    SportName = fixture.SportName,
                    LeagueId = fixture.LeagueId,
                    LeagueName = fixture.LeagueName,
                    SegmentId = fixture.SegmentId,
                    SegmentName = fixture.SegmentName,
                    HomeName = fixture.HomeName,
                    IsBetStopGlobal = fixture.IsBetStopGlobal,
                    Score = fixture.Score,
                    SortOrder = fixture.SortOrder,
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
                            }).ToList(),
                            IsBetStop = fixture.IsBetStopGlobal || (fixture.BetStop.Count > 0 && fixture.BetStop.FirstOrDefault() == 1)
                        }
                    }).ToList(),
                    CloseMatchClick = e => HandleCloseMatchClick(fixture.FixtureName, fixture.ProviderFixtureId)
                });
            }

            return result;
        }
        private async Task ProcessOddsChange(OddsChangeMonitoringMessage message)
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

                    _fixtureMarkets.Add(new MonitoringFixtureModel()
                    {
                        SportId = message.SportId,
                        SportName = message.SportName,
                        SegmentId = message.SegmentId,
                        SegmentName = message.SegmentName,
                        LeagueId = message.LeagueId,
                        LeagueName = message.LeagueName,
                        ProviderFixtureId = message.ProviderFixtureId,
                        FixtureId = message.FixtureId,
                        FixtureName = message.FixtureName,
                        HomeName = message.HomeName,
                        AwayName = message.AwayName,
                        FixtureStartDate = message.FixtureStartDate,
                        IsLive = message.IsLive,
                        //IsLiveStream = message.IsLiveStream,
                        IsSuspended = message.IsSuspended,
                        FixtureStatusId = message.FixtureStatusId,
                        FixtureStatusName = message.FixtureStatusName,
                        Score = message.Score,
                        CurrentPeriod = message.CurrentPeriod,
                        //CurrentMinute = message.CurrentMinute,
                        BetStop = message.BetStop,
                        IsBetStopGlobal = message.IsBetStopGlobal,
                        SortOrder = message.SortOrder,
                        FixtureTypeId = message.FixtureTypeId,
                        UnixTime = message.UnixTime,
                        ClientMarkets = message.Clients.Select(client => new MonitoringFixtureClient()
                        {
                            ClientCode = client.ClientCode,
                            UnixTime = client.UnixTime,
                            Market = new MonitoringFixtureMarketModel()
                            {
                                ClientCode = client.Market.ClientCode,
                                ClientCodeMargin = client.Market.ClientCodeMargin,
                                ProviderFixtureId = client.Market.ProviderFixtureId,
                                MarketTemplateId = client.Market.MarketTemplateId,
                                MarketId = client.Market.MarketId,
                                ProviderMarketId = client.Market.ProviderMarketId,
                                MarketName = client.Market.MarketName,
                                Line = client.Market.Line,
                                MarketStatusId = client.Market.MarketStatusId,
                                MarketStatusName = client.Market.MarketStatusName,
                                MarketSpecifier = client.Market.MarketSpecifier,
                                IsBoosted = client.Market.IsBoosted,
                                UnixTime = client.Market.UnixTime,
                                Selections = client.Market.Selections.Select(selection => new MonitoringFixtureSelectionModel()
                                {
                                    ProviderSelectionId = selection.ProviderSelectionId,
                                    SelectionId = selection.SelectionId,
                                    SelectionTemplateId = selection.SelectionTemplateId,
                                    SelectionName = selection.SelectionName,
                                    SelectionSpecifier = selection.SelectionSpecifier,
                                    DecimalOdds = selection.DecimalOdds,
                                    IsActive = selection.IsActive,
                                    SortOrder = selection.SortOrder,
                                    UnixTime = selection.UnixTime
                                }).ToList()
                            }
                        }).ToList(),
                        CloseMatchClick = e => HandleCloseMatchClick(message.FixtureName, message.ProviderFixtureId)
                    });
                }
                else
                {
                    isReOrder = monitoringFixture.FixtureStatusId != message.FixtureStatusId && message.FixtureStatusId == (int)FixtureStatus.Live;

                    monitoringFixture.CurrentPeriod = message.CurrentPeriod;
                    monitoringFixture.CurrentMinute = message.CurrentMinute.ToString();
                    monitoringFixture.Score = message.Score;
                    monitoringFixture.FixtureStatusId = message.FixtureStatusId;
                    monitoringFixture.UnixTime = message.UnixTime;

                    var markets = new List<MonitoringFixtureClient>();

                    foreach (var client in message.Clients)
                    {
                        var monitoringClient = monitoringFixture.ClientMarkets.FirstOrDefault(c => c.ClientCode == client.ClientCode);

                        if (monitoringClient?.Market != null)
                        {
                            monitoringClient.UnixTime = client.UnixTime;

                            var clientMarket = client.Market;
                            if (monitoringClient.Market.ProviderMarketId == client.Market.ProviderMarketId)
                            {
                                monitoringClient.Market.Line = clientMarket.Line;
                                monitoringClient.Market.MarketStatusId = clientMarket.MarketStatusId;
                                monitoringClient.Market.MarketStatusName = clientMarket.MarketStatusName;
                                monitoringClient.Market.IsBoosted = clientMarket.IsBoosted;
                                monitoringClient.Market.UnixTime = clientMarket.UnixTime;
                                monitoringClient.Market.Selections = clientMarket.Selections.Select(selection => new MonitoringFixtureSelectionModel()
                                {
                                    ProviderSelectionId = selection.ProviderSelectionId,
                                    SelectionId = selection.SelectionId,
                                    SelectionTemplateId = selection.SelectionTemplateId,
                                    SelectionName = selection.SelectionName,
                                    SelectionSpecifier = selection.SelectionSpecifier,
                                    DecimalOdds = selection.DecimalOdds,
                                    IsActive = selection.IsActive,
                                    SortOrder = selection.SortOrder,
                                    UnixTime = selection.UnixTime
                                }).ToList();
                            }
                        }
                        else
                        {
                            monitoringFixture.ClientMarkets.Add(new MonitoringFixtureClient()
                            {

                                ClientCode = client.ClientCode,
                                UnixTime = client.UnixTime,
                                Market = new MonitoringFixtureMarketModel()
                                {
                                    ClientCode = client.Market.ClientCode,
                                    ClientCodeMargin = client.Market.ClientCodeMargin,
                                    ProviderFixtureId = client.Market.ProviderFixtureId,
                                    MarketTemplateId = client.Market.MarketTemplateId,
                                    MarketId = client.Market.MarketId,
                                    ProviderMarketId = client.Market.ProviderMarketId,
                                    MarketName = client.Market.MarketName,
                                    Line = client.Market.Line,
                                    MarketStatusId = client.Market.MarketStatusId,
                                    MarketStatusName = client.Market.MarketStatusName,
                                    MarketSpecifier = client.Market.MarketSpecifier,
                                    IsBoosted = client.Market.IsBoosted,
                                    UnixTime = client.Market.UnixTime,
                                    Selections = client.Market.Selections.Select(selection => new MonitoringFixtureSelectionModel()
                                    {
                                        ProviderSelectionId = selection.ProviderSelectionId,
                                        SelectionId = selection.SelectionId,
                                        SelectionTemplateId = selection.SelectionTemplateId,
                                        SelectionName = selection.SelectionName,
                                        SelectionSpecifier = selection.SelectionSpecifier,
                                        DecimalOdds = selection.DecimalOdds,
                                        IsActive = selection.IsActive,
                                        SortOrder = selection.SortOrder,
                                        UnixTime = selection.UnixTime
                                    }).ToList()
                                }
                            });
                        }
                    }
                }

                if (isReOrder)
                    _fixtureMarkets = _fixtureMarkets.OrderByDescending(o => o.FixtureStatusId).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();
            }

            _shouldRender = true;
            await InvokeAsync(StateHasChanged);
        }

        private List<ClientMarket> GetClientMarket(OddsChangeFixtureDetailsMessage fixtureMessage)
        {
            var result = new List<ClientMarket>();


            return result;
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

        private async Task GetMostEvenLine()
        {

        }

        #endregion

    }
}
