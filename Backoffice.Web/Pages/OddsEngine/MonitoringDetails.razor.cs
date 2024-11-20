using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.OddsEngine.MonitoringDetails;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MonitoringDetails : IAsyncDisposable, IDisposable
    {
        #region Properties

        [Inject] public ICommonService CommonService { get; set; }
        [Inject] public IConfiguration Configuration { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }



        [Parameter] public string Region { get; set; } = "hk";
        [Parameter] public string Id { get; set; }

        private string _region = "hk";
        private HubConnection? _hubConnection { get; set; }

        private List<BreadcrumbModel> Breadcrumbs { get; set; }
        private MarketFixtureResponse.FixtureModel Fixture { get; set; }
        private List<FixtureMarketModel> FixtureMarkets { get; set; }
        private List<FixtureMarketModel> FixtureMarketTemplates { get; set; }
        private List<MarketNavigationModel2> MarketTemplateNavigation { get; set; }
        private MarketModel MarginMarket { get; set; }
        private int SportId { get; set; }
        private string Search { get; set; }
        private string MarketTemplateId { get; set; }
        private List<AlertModel> Alerts { get; set; }
        private decimal Opacity { get; set; }

        #endregion

        #region Fields
        private bool _isPreLoader;
        private bool _isMatchSelectionPreLoad;
        private PageHeaderModel _pageHeaderModel;
        private bool _isPlayerProps;
        private List<Client>? _clients;
        private LanguageCode _languageCode;
        private string _id;
        private int _colCtr = 3;
        private MarketGroupNavigationModel _navigationModel;

        private List<string> _channels = new List<string>();

        private bool _shouldRender = true;
        private bool _init = true;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _isPreLoader = true;
            _clients = new();
            _pageHeaderModel = new();
            TradingService.Region = Region.ToUpper();
            Breadcrumbs = new List<BreadcrumbModel>();
            Fixture = new MarketFixtureResponse.FixtureModel();
            FixtureMarkets = new List<FixtureMarketModel>();
            FixtureMarketTemplates = new List<FixtureMarketModel>();
            MarketTemplateNavigation = new List<MarketNavigationModel2>();
            MarginMarket = new MarketModel();

            await _hubService.InitializeOeMessageHandlers(
                ProcessOddsChangeMonitoring,
                ProcessOnBetStopFixture,
                ProcessOnBetStopGlobal
            );

            await PageState.AuthenticateAccessEvent.InvokeAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            _shouldRender = false;

            if (firstRender)
            {
                try
                {
                    //await Subscribe();
                }
                catch (Exception ex)
                {
                    Log.Error($"Hubconnection URI - {NavigationManager.BaseUri}");
                    Console.WriteLine(ex.Message);

                    Log.Error(ex.Message);
                    if (ex.StackTrace != null)
                        Log.Error(ex.StackTrace.ToString());
                }
            }
        }
        protected override async Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameters();
            if (_shouldRender)
            {
                _isPreLoader = true;
                _id = Id;
                if (_languageCode != _translationState.ActiveLanguageCode)
                    await PrepareMonitoringDetails();

                _languageCode = _translationState.ActiveLanguageCode;

                _isPreLoader = false;
                _init = false;
            }
        }
        public async Task OnMarketChangedEvent(string marketTemplateId)
        {
            _isPreLoader = true;
            _shouldRender = true;

            if (_languageCode != _translationState.ActiveLanguageCode)
            {
                _isMatchSelectionPreLoad = true;
                _id = Id;
                _languageCode = _translationState.ActiveLanguageCode;

                await PrepareMonitoringDetails();

                FixtureMarketTemplates = await PrepareFixtureMarketTemplates(marketTemplateId);
                _isMatchSelectionPreLoad = true;
                await OnMarketTemplateGroupClickEvent(_navigationModel);
            }

            MarketTemplateId = marketTemplateId;

            var markets = Fixture.Markets.Where(c => c.MarketTemplateId == MarketTemplateId).ToList();


            FixtureMarkets = await PrepareFixtureMarkets(markets);

            MarginMarket = markets.FirstOrDefault(f => f.MarketStatusId == (int)MarketStatus.Active) ?? markets.FirstOrDefault() ?? new();
            _shouldRender = true;
            _isPreLoader = false;
        }
        public void OnCloseMarkeEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("Fixture", Fixture);
            //parameters.Add("OnCancelCallback", EventCallback.Factory.Create(this, UndoEditedMarkets));
            parameters.Add("Region", Region);

            var options = new ModalOptions();
            ModalService.Open<CloseMarketsModal>("Close Markets", parameters, options);
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

            return markets.FirstOrDefault(c => c.MarketId == nearestSelection?.MarketId);
        }


        private async Task FilterClientCallback(List<SelectListItemWithCheckboxModel<string>> clients)
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
            }

            _shouldRender = true;
        }

        public async Task OnInputSearchEvent(string search)
        {
            _isMatchSelectionPreLoad = true;
            _shouldRender = true;
            Search = search;

            var selectedNavigation = MarketTemplateNavigation.FirstOrDefault(c => c.IsSelected);

            FixtureMarketTemplates = await PrepareFixtureMarketTemplates(selectedNavigation.MarketTemplateId);

            _shouldRender = true;
            _isMatchSelectionPreLoad = false;
        }

        public async Task OnMarketTemplateGroupClickEvent(MarketGroupNavigationModel model)
        {
            _isMatchSelectionPreLoad = true;
            _shouldRender = true;
            _navigationModel = model;

            var marketTemplateIds = model.MarketTemplateGroup.MarketTemplateIds;
            var markets = Fixture.Markets.Where(c => marketTemplateIds.Contains(c.MarketTemplateId)).ToList();
            //var markets = Fixture.Markets.Where(c => c.MarketId == model.MarketTemplateGroup.marke).ToList();

            // display list of markets available on the selected group
            MarketTemplateNavigation = markets.Select((s, index) => new MarketNavigationModel2
            {
                IsSelected = index == 0 ? true : false,
                MarketTemplateId = s.MarketTemplateId,
                MarketId = s.MarketId,
                MarketName = s.MarketName,
            })
            .DistinctBy(d => new { d.MarketTemplateId, d.MarketName })
            .ToList();

            var selectedMarket = MarketTemplateNavigation.FirstOrDefault(c => c.IsSelected);

            if (selectedMarket != null)
                await OnMarketClickEvent(selectedMarket.MarketId);

            _shouldRender = true;
            _isMatchSelectionPreLoad = false;
        }

        public async Task OnMarketClickEvent(string marketId)
        {
            if (!_isMatchSelectionPreLoad)
            {
                _isMatchSelectionPreLoad = true;
            }

            var markets = Fixture.Markets.Where(c => c.MarketId == marketId).ToList();
            FixtureMarketTemplates = await PrepareFixtureMarkets(markets);
            _isMatchSelectionPreLoad = false;
        }

        public async Task OnSaveClickEvent(object item)
        {
            Opacity = Opacity == 1m ? 0.99m : 1m;

            Alerts.Clear();
            Alerts.Add(new AlertModel
            {
                Status = AlertStatus.Success,
                Message = (bool)item ? "Selected Market has been closed successfully" : "Client successfully created",
                Opacity = Opacity
            });

            _shouldRender = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }
            }
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private bool ValidateParameters() => _init || _languageCode != _translationState.ActiveLanguageCode || Id != _id;
        private async Task PrepareMonitoringDetails()
        {
            var marketFixtureResponse = await BackofficeService.GetMarketFixtureDetails(Id, _languageCode);

            if (marketFixtureResponse.StatusCode == StatusCode.Success && marketFixtureResponse.Result.Fixture != null)
            {
                Fixture = marketFixtureResponse.Result.Fixture;
                await SubscribeToChannel($"fixturedetails:{Fixture.FixtureId}");
                SportId = Fixture.SportId;
                _pageHeaderModel = new()
                {
                    Title = $"{_translationState.GetValue(Fixture.SportName)} - {Fixture.SegmentName} - {Fixture.LeagueName}",
                    IconClass = $"iconfont iconfont-{Utility.GetSportIcon(Fixture?.SportId ?? 0)}",
                    ShowLanguageDropDown = false
                };

                Breadcrumbs = new()
                {
                    new BreadcrumbModel
                    {
                        Name = "Monitoring",
                        Url = $"{Region}/odds-engine/monitoring",
                        IsCurrentPage = false
                    },
                    new BreadcrumbModel
                    {
                        Name = Fixture.SportName,
                        //Url = $"odds-engine/monitoring/{Fixture.SportId}",
                        Url = "",
                        IsCurrentPage = false
                    },
                    new BreadcrumbModel
                    {
                        Name = Fixture.SegmentName,
                        //Url = $"odds-engine/monitoring/{Fixture.SportId}/{Fixture.SegmentId}",
                        Url = "",
                        IsCurrentPage = false
                    },new BreadcrumbModel
                    {
                        Name = Fixture.LeagueName,
                        //Url = $"odds-engine/monitoring/{Fixture.SportId}/{Fixture.SegmentId}/{Fixture.LeagueId}",
                        Url = "",
                        IsCurrentPage = true
                    }
                };
            }
        }
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

        private async Task<List<FixtureMarketModel>> PrepareFixtureMarkets(List<MarketModel> markets) => markets.Select(s => new FixtureMarketModel
        {
            FixtureName = Fixture.FixtureName,
            MarketId = s.MarketId,
            ClientCode = s.ClientCode,
            ClientCodeMargin = s.ClientCodeMargin,
            MarketName = s.MarketName,
            ProviderMarketId = s.ProviderMarketId,
            ProviderFixtureId = s.ProviderFixtureId,
            AwayName = Fixture.AwayName,
            BetStop = Fixture.BetStop,
            CurrentMinute = Fixture.CurrentMinute,
            CurrentPeriod = Fixture.CurrentPeriod,
            FixtureStartDate = Fixture.FixtureStartDate,
            FixtureStatusId = Fixture.FixtureStatusId,
            HomeName = Fixture.HomeName,
            IsBetStopGlobal = Fixture.IsBetStopGlobal,
            Score = Fixture.Score,
            Line = s.Line,
            //MarketSpecifier = s.MarketSpecifier,
            MarketStatusId = s.MarketStatusId,
            //MarketStatusName = s.MarketStatusName,
            CorrelationId = Guid.NewGuid().ToString(),
            MarketTemplateId = s.MarketTemplateId,
            UnixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Selections = s.Selections.Select(se => new SelectionModel
            {
                ProviderSelectionId = se.ProviderSelectionId,
                SelectionId = se.SelectionId,
                SelectionTemplateId = se.SelectionTemplateId,
                SelectionName = se.SelectionName,
                SelectionSpecifier = se.SelectionSpecifier,
                DecimalOdds = se.DecimalOdds,
                IsActive = se.IsActive,
                SortOrder = se.SortOrder,
                CorrelationId = se.CorrelationId,
                UnixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ClientOdds = se.ClientOdds.Select(od => new ClientOddsModel
                {
                    ClientCode = od.ClientCode,
                    Odds = od.Odds,
                    SelectionTemplateId = od.SelectionTemplateId,
                    SelectionId = od.SelectionId
                }).ToList()
            }).ToList()
        }).OrderBy(o => string.IsNullOrEmpty(o.Line) ? 0 : Convert.ToDecimal(o.Line)).ToList();

        private async Task<List<FixtureMarketModel>> PrepareFixtureMarketTemplates(string marketTemplateId)
        {
            var filteredMarkets = Fixture.Markets.Where(c => c.MarketTemplateId == marketTemplateId);

            //if (!string.IsNullOrEmpty(Search))
            //{
            //    filteredMarkets = filteredMarkets.Select(s => new MarketModel
            //    {
            //        ProviderFixtureId = s.ProviderFixtureId,
            //        MarketTemplateId = s.MarketTemplateId,
            //        MarketId = s.MarketId,
            //        ProviderMarketId = s.ProviderMarketId,
            //        MarketName = s.MarketName,
            //        Line = s.Line,
            //        MarketStatusId = s.MarketStatusId,
            //        MarketStatusName = s.MarketStatusName,
            //        MarketSpecifier = s.MarketSpecifier,
            //        ClientCode = s.ClientCode,
            //        ClientCodeMargin = s.ClientCodeMargin,
            //        IsBoosted = s.IsBoosted,
            //        Selections = s.Selections.Where(se => se.SelectionName.ToLower().Contains(Search.ToLower())).ToList(),
            //    }).Where(c => c.Selections.Any());
            //}

            MarginMarket = filteredMarkets.FirstOrDefault(f => f.MarketStatusId == (int)MarketStatus.Active) ?? filteredMarkets.FirstOrDefault() ?? new();

            return await PrepareFixtureMarkets(filteredMarkets.ToList());
        }

        private async Task ProcessOddsChangeMonitoring(OddsChangeFixtureMessage message)
        {
            if (Fixture == null || Fixture.FixtureTypeId != message.FixtureTypeId || Id != message.ProviderFixtureId)
                return;

            //if (MarketGroupCallback == null || MarketGroupCallback.MarketTemplateGroups == null || !MarketGroupCallback.MarketTemplateGroups.Any())
            //    return;

            var newMarketTemplateIds = message.Markets.Select(s => s.MarketTemplateId).ToList();
            var marketFixtures = Fixture.Markets.Where(c => newMarketTemplateIds.Contains(c.MarketTemplateId)).ToList();

            foreach (var marketFixture in marketFixtures)
            {
                Fixture.BetStop = new List<int>();
                Fixture.CurrentPeriod = message.CurrentPeriod;
                Fixture.CurrentMinute = message.CurrentMinute.ToString();
                Fixture.Score = message.Score;
                Fixture.FixtureStatusId = message.FixtureStatusId;
                Fixture.UnixTime = message.UnixTime;

                foreach (var newMarket in message.Markets.Where(c => c.MarketTemplateId == marketFixture.MarketTemplateId && c.MarketName == marketFixture.MarketName && c.MarketSpecifier == marketFixture.MarketSpecifier))
                {
                    var selectionIds = newMarket.Selections.Select(s => s.SelectionTemplateId).Distinct().ToList();

                    var providerMarketId = $"{Fixture.ProviderFixtureId}:{newMarket.MarketTemplateId}:{newMarket.MarketSpecifier}";
                    var currentMarket = Fixture.Markets.FirstOrDefault(c => c.ProviderMarketId == providerMarketId && c.MarketName == newMarket.MarketName);

                    if (currentMarket != null)
                    {
                        currentMarket.MarketStatusId = newMarket.MarketStatusId;
                        currentMarket.MarketStatusName = ((MarketStatus)newMarket.MarketStatusId).ToString();
                        currentMarket.ClientCode = newMarket.ClientCode;
                        currentMarket.ClientCodeMargin = newMarket.ClientCodeMargin;
                        currentMarket.UnixTime = newMarket.UnixTime;

                        foreach (var newSelection in newMarket.Selections)
                        {
                            var providerSelectionId = $"{Fixture.ProviderFixtureId}:{newMarket.MarketTemplateId}:{newSelection.SelectionTemplateId}:{newMarket.MarketSpecifier}";
                            var currentSelection = currentMarket.Selections.FirstOrDefault(c => c.ProviderSelectionId == providerSelectionId);

                            if (currentSelection != null)
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

                                if (newSelection.IsActive)
                                {
                                    currentSelection.DecimalOdds = CommonService.GetFinalOdds(newSelection.DecimalOdds).ToString("0.00");
                                    currentSelection.IsActive = newSelection.IsActive;
                                    currentSelection.UnixTime = newSelection.UnixTime;
                                    currentSelection.ClientOdds = clientOdds;
                                    //currentSelection.ClientOdds = newSelection.ClientOdds.Select(s =>
                                    //    new ClientOddsModel
                                    //    {
                                    //        ClientCode = s.ClientCode,
                                    //        SelectionTemplateId = s.SelectionTemplateId,
                                    //        Odds = s.DecimalOdds,
                                    //        OldOdds = currentSelection.ClientOdds != null && currentSelection.ClientOdds.Any(c => c.ClientCode == s.ClientCode) ?
                                    //                    currentSelection.ClientOdds.Where(c => c.ClientCode == s.ClientCode).FirstOrDefault().Odds : s.DecimalOdds
                                    //    }).ToList();
                                }
                            }
                        }
                    }
                    else
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
                                ProviderSelectionId = $"{Fixture.ProviderFixtureId}:{newMarket.MarketTemplateId}:{newSelection.SelectionTemplateId}:{newMarket.MarketSpecifier}",
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
                        // add market
                        var market = new MarketModel()
                        {
                            MarketId = newMarket.MarketId.ToString(),
                            ProviderMarketId = providerMarketId,
                            ProviderFixtureId = message.ProviderFixtureId,
                            Line = newMarket.Line,
                            ClientCode = newMarket.ClientCode,
                            ClientCodeMargin = newMarket.ClientCodeMargin,
                            MarketStatusId = newMarket.MarketStatusId,
                            MarketStatusName = ((MarketStatus)newMarket.MarketStatusId).ToString(),
                            MarketName = newMarket.MarketName,
                            MarketTemplateId = newMarket.MarketTemplateId,
                            UnixTime = newMarket.UnixTime,
                            Selections = selections
                        };

                        Fixture.Markets.Add(market);
                    }
                }
            }

            var selectedNavigation = MarketTemplateNavigation.FirstOrDefault(c => c.IsSelected);

            if (newMarketTemplateIds.Contains(MarketTemplateId) || (selectedNavigation != null && newMarketTemplateIds.Contains(selectedNavigation.MarketTemplateId)))
            {
                if (newMarketTemplateIds.Contains(MarketTemplateId))
                {
                    var markets = Fixture.Markets.Where(c => c.MarketTemplateId == MarketTemplateId).ToList();
                    FixtureMarkets = await PrepareFixtureMarkets(markets);
                }

                if (newMarketTemplateIds.Contains(selectedNavigation.MarketTemplateId))
                {
                    FixtureMarketTemplates = await PrepareFixtureMarketTemplates(selectedNavigation.MarketTemplateId);
                }


                _shouldRender = true;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task ProcessOnBetStopFixture(BetStopFixtureMessage message)
        {
            if (Fixture == null || Id != message.ProviderFixtureId)
                return;

            Fixture.BetStop = message.IsBetStop ? new List<int>() { 1 } : new List<int>();

            FixtureMarkets.ForEach(m =>
            {
                m.BetStop = message.IsBetStop ? new List<int>() { 1 } : new List<int>();
            });

            _shouldRender = true;
            await InvokeAsync(StateHasChanged);
        }

        private async Task ProcessOnBetStopGlobal(BetStopGlobalMessage message)
        {
            //if (MarketTemplates == null)
            //    return;

            Fixture.IsBetStopGlobal = message.IsBetStop;

            FixtureMarkets.ForEach(m =>
            {
                m.IsBetStopGlobal = message.IsBetStop;
            });

            _shouldRender = true;
            await InvokeAsync(StateHasChanged);
        }

        protected string GetClientMargin(string clientCode, string clientCodeMargin, List<SelectionModel> selections, bool isPlayerProps)
        {
            var clients = string.IsNullOrEmpty(clientCodeMargin) ? new List<Tuple<string, decimal>>() : clientCodeMargin.Split('|').Select(s => new Tuple<string, decimal>(s.Split(':').First(), Convert.ToDecimal(s.Split(':').Last()))).ToList();
            var client = clients.FirstOrDefault(c => c.Item1 == clientCode);

            if (client == null)
            {
                var activeSelections = new List<SelectionModel>();

                if (isPlayerProps)
                {
                    if (selections.Any(c => c.SelectionTemplateId != "12" && c.SelectionTemplateId != "13"))
                    {
                        return "100";
                    }
                }
                else
                {
                    activeSelections = selections.Where(c => c.IsActive == true && Convert.ToDecimal(GetClientOdds(c, clientCode)) > 0m).ToList();
                }

                if (activeSelections.Count == 1)
                    activeSelections = selections.Where(c => Convert.ToDecimal(GetClientOdds(c, clientCode)) > 0m).ToList();

                var margin = (Math.Round(activeSelections.Sum(s => 100 / Convert.ToDecimal(GetClientOdds(s, clientCode)))) - 100) / 100;

                return ((int)((margin * 100) + 100)).ToString();
            }

            var newMargin = ((int)((client.Item2 * 100) + 100)).ToString();

            var newactiveSelections = new List<SelectionModel>();

            if (isPlayerProps && selections.Count() != 2)
            {
                var newactiveSelection = selections.FirstOrDefault(c => c.IsActive == true);

                if (newactiveSelection != null)
                {
                    newactiveSelections.Add(newactiveSelection);

                    var underOdds = 1 / (1 - (1 / Convert.ToDecimal(newactiveSelection.DecimalOdds)));

                    newactiveSelections.Add(new SelectionModel
                    {
                        DecimalOdds = underOdds.ToString(),
                        ClientOdds = new List<ClientOddsModel>() { GetPlayerPropClientOdds(underOdds, client) },
                    });
                }
            }
            else
            {
                newactiveSelections = selections.Where(c => c.IsActive == true).ToList();

                if (newactiveSelections.Count == 1)
                    newactiveSelections = selections.Where(c => Convert.ToDecimal(GetClientOdds(c, clientCode)) > 0m).ToList();

            }

            var computedMargin = $"{(Math.Round(newactiveSelections.Sum(s => 100 / s.ClientOdds.FirstOrDefault(c => c.ClientCode == clientCode && c.SelectionTemplateId == s.SelectionTemplateId).Odds)) - 100) + 100}";

            if (newMargin != computedMargin)
                newMargin = $"{newMargin} ({computedMargin})";

            return newMargin;
        }

        protected string GetClientOdds(SelectionModel selection, string clientCode)
        {
            if (selection.ClientOdds != null && selection.ClientOdds.Any(c => c.ClientCode == clientCode))
            {
                return selection.ClientOdds.FirstOrDefault(c => c.ClientCode == clientCode && c.SelectionTemplateId == selection.SelectionTemplateId).Odds.ToString();
            }

            return selection.DecimalOdds;
        }

        protected bool HasClientConfig(string clientCode, string marketClientCode, string marketClientMargin)
        {
            if (string.IsNullOrEmpty(marketClientCode))
                marketClientCode = string.Empty;

            if (string.IsNullOrEmpty(marketClientMargin))
                marketClientMargin = string.Empty;

            if (marketClientCode.Split("|").Contains(clientCode) || marketClientMargin.Contains(clientCode))
            {
                return true;
            }

            return false;
        }

        private ClientOddsModel GetPlayerPropClientOdds(decimal underOdds, Tuple<string, decimal> client)
        {
            var fairOdds = underOdds;
            var finalOddsInitialDivideBy = ((fairOdds * client.Item2) / 2) + 1;

            var clientUnderOdds = fairOdds / finalOddsInitialDivideBy;

            return new ClientOddsModel
            {
                ClientCode = client.Item1,
                Odds = clientUnderOdds,
                //OldOdds = clientUnderOdds,
            };
        }

        #endregion

    }
}
