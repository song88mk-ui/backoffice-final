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

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class Monitoring : IDisposable
    {
        #region Properties
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
            _slugs = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "").Split("/").ToList();

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
                            //StrClientCodeMargin = GetClientMargin(clientMarket.Market.ClientCodeMargin, clientMarket.Market.Selections),
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
        private string GetClientMargin(decimal clientCodeMargin, List<SelectionModelV2> selections)
        {
            if (clientCodeMargin > 0)
            {
                var newMargin = ((int)((clientCodeMargin * 100) + 100)).ToString();

                var newActiveSelections = selections.Where(c => c.IsActive == true).ToList();

                if (newActiveSelections.Count == 1)
                    newActiveSelections = selections.Where(c => c.IsActive == true).ToList();

                var computedMargin = "";
                try
                {
                    computedMargin = $"{(Math.Round(newActiveSelections.Sum(s => 100 / Convert.ToDecimal(s.DecimalOdds))) - 100) + 100}";
                }
                catch (Exception ex)
                {

                }
                if (newMargin != computedMargin)
                {
                    newMargin = $"{newMargin} ({computedMargin})";
                }
                return newMargin;
            }

            var activeSelections = selections.Where(c => c.IsActive == true && Convert.ToDecimal(c.DecimalOdds) > 0m).ToList();

            if (activeSelections.Count == 1)
                activeSelections = selections.Where(c => Convert.ToDecimal(Convert.ToDecimal(c.DecimalOdds)) > 0m).ToList();

            var margin = (Math.Round(activeSelections.Sum(s => 100 / Convert.ToDecimal(s.DecimalOdds))) - 100) / 100;

            return ((int)((margin * 100) + 100)).ToString();
        }
        private string GetClientMargin(decimal clientCodeMargin, List<OddsChangeMonitoringSelection> selections)
        {
            if (clientCodeMargin > 0)
            {
                var newMargin = ((int)((clientCodeMargin * 100) + 100)).ToString();

                var newActiveSelections = selections.Where(c => c.IsAc == true).ToList();

                if (newActiveSelections.Count == 1)
                    newActiveSelections = selections.Where(c => c.IsAc == true).ToList();

                var computedMargin = "";
                try
                {
                    computedMargin = $"{(Math.Round(newActiveSelections.Sum(s => 100 / Convert.ToDecimal(s.DeOd))) - 100) + 100}";
                }
                catch (Exception ex)
                {

                }
                if (newMargin != computedMargin)
                {
                    newMargin = $"{newMargin} ({computedMargin})";
                }
                return newMargin;
            }

            var activeSelections = selections.Where(c => c.IsAc == true && Convert.ToDecimal(c.DeOd) > 0m).ToList();

            if (activeSelections.Count == 1)
                activeSelections = selections.Where(c => Convert.ToDecimal(c.DeOd) > 0m).ToList();

            var margin = (Math.Round(activeSelections.Sum(s => 100 / Convert.ToDecimal(s.DeOd))) - 100) / 100;

            return ((int)((margin * 100) + 100)).ToString();
        }
        private async Task ProcessOddsChange(OddsChangeMonitoringMessage message)
        {
            if (_fixtureMarkets == null)
                return;

            if (_sportId != message.SpId || _segmentId != message.SegId || _leagueId != message.LeId)
                return;

            if (message.FiTyId != (int)FixtureType.Match)
                return;

            if (message.FiStId > (int)FixtureStatus.Live)
            {
                var fixture = _fixtureMarkets.FirstOrDefault(c => c.ProviderFixtureId == message.PrFiId);

                if (fixture != null)
                    _fixtureMarkets.Remove(fixture);
            }
            else
            {
                var isReOrder = false;
                var monitoringFixture = _fixtureMarkets.FirstOrDefault(c => c.ProviderFixtureId == message.PrFiId);

                if (monitoringFixture == null)
                {
                    isReOrder = true;

                    _fixtureMarkets.Add(new MonitoringFixtureModel()
                    {
                        SportId = message.SpId,
                        SportName = message.SpNa,
                        SegmentId = message.SegId,
                        SegmentName = message.SegNa,
                        LeagueId = message.LeId,
                        LeagueName = message.LeNa,
                        ProviderFixtureId = message.PrFiId,
                        FixtureId = message.FiId,
                        FixtureName = message.FiNa,
                        HomeName = message.HoNa,
                        AwayName = message.AwNa,
                        FixtureStartDate = message.FiStDa,
                        IsLive = message.IsLi,
                        //IsLiveStream = message.IsLiveStream,
                        //IsSuspended = message.IsLi,
                        FixtureStatusId = message.FiStId,
                        FixtureStatusName = message.FiStNa,
                        Score = message.Sc,
                        CurrentPeriod = message.CuPe,
                        CurrentMinute = message.CuMi.ToString(),
                        //BetStop = message.BetStop,
                        //IsBetStopGlobal = message.IsBetStopGlobal,
                        SortOrder = message.SoOr,
                        FixtureTypeId = message.FiTyId,
                        UnixTime = message.UnTi,
                        ClientMarkets = message.Cl.Select(client => new MonitoringFixtureClient()
                        {
                            ClientCode = client.ClCo,
                            UnixTime = client.UnTi,
                            Market = client.Ma.Select(market => new MonitoringFixtureMarketModel()
                            {
                                ClientCode = market.ClCo,
                                ClientCodeMargin = market.ClCoMa,
                                //StrClientCodeMargin = GetClientMargin(market.ClCoMa, market.Se),
                                ProviderFixtureId = market.PrFiId,
                                MarketTemplateId = market.MaTeId,
                                MarketId = market.MaId,
                                ProviderMarketId = market.PrMaId,
                                MarketName = market.MaNa,
                                Line = market.Li,
                                MarketStatusId = market.MaStId,
                                MarketStatusName = market.MaStNa,
                                MarketSpecifier = market.MaSp,
                                IsBoosted = market.IsBo,
                                UnixTime = market.UnTi,
                                Selections = market.Se.Select(selection => new MonitoringFixtureSelectionModel()
                                {
                                    ProviderSelectionId = selection.PrSeId,
                                    SelectionId = selection.SeId,
                                    SelectionTemplateId = selection.SeTeId,
                                    SelectionName = selection.SeNa,
                                    SelectionSpecifier = selection.SeSp,
                                    DecimalOdds = selection.DeOd,
                                    IsActive = selection.IsAc,
                                    SortOrder = selection.SoOr,
                                    UnixTime = selection.UnTi
                                }).ToList()
                            }).FirstOrDefault(market => market.MarketTemplateId == _monitoringCallbackModel.SelectedMarketTemplateId)
                        }).ToList(),
                        CloseMatchClick = e => HandleCloseMatchClick(message.FiNa, message.PrFiId)
                    });
                }
                else
                {
                    isReOrder = monitoringFixture.FixtureStatusId != message.FiStId && message.FiStId == (int)FixtureStatus.Live;

                    monitoringFixture.CurrentPeriod = message.CuPe;
                    monitoringFixture.CurrentMinute = message.CuMi.ToString();
                    monitoringFixture.FixtureStartDate = message.FiStDa;
                    monitoringFixture.Score = message.Sc;
                    monitoringFixture.FixtureStatusId = message.FiStId;
                    monitoringFixture.UnixTime = message.UnTi;

                    //var markets = new List<MonitoringFixtureClient>();

                    foreach (var client in message.Cl)
                    {
                        if (client.Ma == null)
                            continue;

                        var clientMarket = client.Ma.FirstOrDefault(market => market.MaTeId == _monitoringCallbackModel.SelectedMarketTemplateId);

                        if (clientMarket == null)
                            continue;

                        var monitoringClient = monitoringFixture.ClientMarkets.FirstOrDefault(c => c.ClientCode == client.ClCo);

                        if (monitoringClient?.Market != null)
                        {
                            monitoringClient.UnixTime = client.UnTi;

                            if (monitoringClient.Market.ProviderMarketId == clientMarket.PrMaId)
                            {
                                monitoringClient.Market.ClientCodeMargin = clientMarket.ClCoMa;
                                //monitoringClient.Market.StrClientCodeMargin = GetClientMargin(clientMarket.ClCoMa, clientMarket.Se);
                                monitoringClient.Market.Line = clientMarket.Li;
                                monitoringClient.Market.MarketStatusId = clientMarket.MaStId;
                                monitoringClient.Market.MarketStatusName = clientMarket.MaStNa;
                                monitoringClient.Market.IsBoosted = clientMarket.IsBo;
                                monitoringClient.Market.UnixTime = clientMarket.UnTi;
                                monitoringClient.Market.Selections = clientMarket.Se.Select(selection => new MonitoringFixtureSelectionModel()
                                {
                                    ProviderSelectionId = selection.PrSeId,
                                    SelectionId = selection.SeId,
                                    SelectionTemplateId = selection.SeTeId,
                                    SelectionName = selection.SeNa,
                                    SelectionSpecifier = selection.SeSp,
                                    DecimalOdds = selection.DeOd,
                                    IsActive = selection.IsAc,
                                    SortOrder = selection.SoOr,
                                    UnixTime = selection.UnTi
                                }).ToList();
                            }
                            else
                            {
                                monitoringClient.Market = new MonitoringFixtureMarketModel()
                                {
                                    ClientCode = clientMarket.ClCo,
                                    ClientCodeMargin = clientMarket.ClCoMa,
                                    //StrClientCodeMargin = GetClientMargin(clientMarket.ClCoMa, clientMarket.Se),
                                    ProviderFixtureId = clientMarket.PrFiId,
                                    MarketTemplateId = clientMarket.MaTeId,
                                    MarketId = clientMarket.MaId,
                                    ProviderMarketId = clientMarket.PrMaId,
                                    MarketName = clientMarket.MaNa,
                                    Line = clientMarket.Li,
                                    MarketStatusId = clientMarket.MaStId,
                                    MarketStatusName = clientMarket.MaStNa,
                                    MarketSpecifier = clientMarket.MaSp,
                                    IsBoosted = clientMarket.IsBo,
                                    UnixTime = clientMarket.UnTi,
                                    Selections = clientMarket.Se.Select(selection => new MonitoringFixtureSelectionModel()
                                    {
                                        ProviderSelectionId = selection.PrSeId,
                                        SelectionId = selection.SeId,
                                        SelectionTemplateId = selection.SeTeId,
                                        SelectionName = selection.SeNa,
                                        SelectionSpecifier = selection.SeSp,
                                        DecimalOdds = selection.DeOd,
                                        IsActive = selection.IsAc,
                                        SortOrder = selection.SoOr,
                                        UnixTime = selection.UnTi
                                    }).ToList()
                                };
                            }
                        }
                        else
                        {
                            monitoringFixture.ClientMarkets.Add(new MonitoringFixtureClient()
                            {
                                ClientCode = client.ClCo,
                                UnixTime = client.UnTi,
                                Market = new MonitoringFixtureMarketModel()
                                {
                                    ClientCode = clientMarket.ClCo,
                                    ClientCodeMargin = clientMarket.ClCoMa,
                                    //StrClientCodeMargin = GetClientMargin(clientMarket.ClCoMa, clientMarket.Se),
                                    ProviderFixtureId = clientMarket.PrFiId,
                                    MarketTemplateId = clientMarket.MaTeId,
                                    MarketId = clientMarket.MaId,
                                    ProviderMarketId = clientMarket.PrMaId,
                                    MarketName = clientMarket.MaNa,
                                    Line = clientMarket.Li,
                                    MarketStatusId = clientMarket.MaStId,
                                    MarketStatusName = clientMarket.MaStNa,
                                    MarketSpecifier = clientMarket.MaSp,
                                    IsBoosted = clientMarket.IsBo,
                                    UnixTime = clientMarket.UnTi,
                                    Selections = clientMarket.Se.Select(selection => new MonitoringFixtureSelectionModel()
                                    {
                                        ProviderSelectionId = selection.PrSeId,
                                        SelectionId = selection.SeId,
                                        SelectionTemplateId = selection.SeTeId,
                                        SelectionName = selection.SeNa,
                                        SelectionSpecifier = selection.SeSp,
                                        DecimalOdds = selection.DeOd,
                                        IsActive = selection.IsAc,
                                        SortOrder = selection.SoOr,
                                        UnixTime = selection.UnTi
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
            StateHasChanged();
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
