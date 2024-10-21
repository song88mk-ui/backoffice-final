using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;
using Backoffice.Web.Components.OddsEngine.Configuration;
using Backoffice.Web.Components.OddsEngine.MonitoringDetails;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;
using Serilog;
using RabbitMQ.Client.Apigen.Attributes;
using static Backoffice.Shared.Models.Response.FastOddsFixturesResponse;
using System.Drawing.Printing;
using System.Drawing;

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
        private PageHeaderModel _pageHeaderModel;
        private bool _isPlayerProps;
        private List<Client>? _clients;
        private LanguageCode _languageCode;
        private bool _isInit;
        private string _id;
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
            _isInit = true;

            await PageState.AuthenticateAccessEvent.InvokeAsync();
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
        protected override async Task OnParametersSetAsync()
        {
            if (_isInit || _languageCode != _translationState.ActiveLanguageCode || Id != _id)
            {
                _id = Id;
                _languageCode = _translationState.ActiveLanguageCode;

                await PrepareMonitoringDetails();
                _isInit = false;
            }
        }

        public async Task OnMarketChangedEvent(string marketTemplateId)
        {
            _isPreLoader = true;
            await InvokeAsync(StateHasChanged);
            MarketTemplateId = marketTemplateId;

            var markets = Fixture.Markets.Where(c => c.MarketTemplateId == MarketTemplateId).ToList();
            FixtureMarkets = await PrepareFixtureMarkets(markets);

            MarginMarket = markets.FirstOrDefault(f => f.MarketStatusId == (int)MarketStatus.Active) ?? markets.FirstOrDefault() ?? new();
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
            }
        }

        public async Task OnInputSearchEvent(string search)
        {
            Search = search;

            var selectedNavigation = MarketTemplateNavigation.FirstOrDefault(c => c.IsSelected);

            FixtureMarketTemplates = PrepareFixtureMarketTemplates(selectedNavigation.MarketTemplateId);
        }

        public async Task OnMarketTemplateGroupClickEvent(MarketGroupNavigationModel model)
        {
            var marketTemplateIds = model.MarketTemplateGroup.MarketTemplateIds;
            var markets = Fixture.Markets.Where(c => marketTemplateIds.Contains(c.MarketTemplateId)).ToList();

            // display list of markets available on the selected group
            MarketTemplateNavigation = markets.Select((s, index) => new MarketNavigationModel2
            {
                IsSelected = index == 0 ? true : false,
                MarketTemplateId = s.MarketTemplateId,
                MarketName = s.MarketName,
            })
            .DistinctBy(d => new { d.MarketTemplateId, d.MarketName })
            .ToList();

            var selectedNavigation = MarketTemplateNavigation.FirstOrDefault(c => c.IsSelected);

            FixtureMarketTemplates = PrepareFixtureMarketTemplates(selectedNavigation.MarketTemplateId);
        }

        public async Task OnMarketTemplateClickEvent(string marketTemplateId)
        {
            FixtureMarketTemplates = PrepareFixtureMarketTemplates(marketTemplateId);
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

        private async Task PrepareMonitoringDetails()
        {
            if (!_isPreLoader)
                _isPreLoader = true;

            var marketFixtureResponse = await BackofficeService.GetMarketFixtureDetails(Id, _languageCode);

            if (marketFixtureResponse.StatusCode == StatusCode.Success)
            {
                Fixture = marketFixtureResponse.Result.Fixture;
                SportId = Fixture.SportId;
                _pageHeaderModel = new()
                {
                    Title = $"{_translationState.GetValue(Fixture.SportName)} - {Fixture.SegmentName} - {Fixture.LeagueName}",
                    IconClass = $"iconfont iconfont-{Fixture.SportName.ToLower().Replace(" ", "-")}",
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

            _isPreLoader = false;
        }

        private async Task<List<FixtureMarketModel>> PrepareFixtureMarkets(List<MarketModel> markets)
        {
            return markets.Select(s => new FixtureMarketModel
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
                MarketTemplateId = s.MarketTemplateId,
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
                    OldDecimalOdds = se.OldDecimalOdds,
                    ClientOdds = se.ClientOdds.Select(od => new ClientOddsModel
                    {
                        ClientCode = od.ClientCode,
                        Odds = od.Odds,
                        OldOdds = od.OldOdds,
                        SelectionTemplateId = od.SelectionTemplateId,
                        SelectionId = od.SelectionId
                    }).ToList()
                }).ToList()
            }).OrderBy(o => string.IsNullOrEmpty(o.Line) ? 0 : Convert.ToDecimal(o.Line)).ToList();
        }

        private List<FixtureMarketModel> PrepareFixtureMarketTemplates(string marketTemplateId)
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

            return PrepareFixtureMarkets(filteredMarkets.ToList()).Result;
        }

        private async Task Subscribe()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(new Uri(Path.Combine(NavigationManager.BaseUri, "bohub")))
            .Build();


            _hubConnection.On<OnExternalOddsChangeMessage>("onoddschange", async (message) =>
            {
                await ProcessOddsChangeMonitoring(message);

            });

            _hubConnection.On<OnExternalBetStopFixtureMessage>("onbetstopfixture", async (message) =>
            {
                await ProcessOnBetStopFixture(message);

            });

            _hubConnection.On<OnExternalBetStopGlobalMessage>("onbetstopglobal", async (message) =>
            {
                await ProcessOnBetStopGlobal(message);

            });

            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync("AddToGroup", Region.ToUpper());
        }

        private async Task ProcessOddsChangeMonitoring(OnExternalOddsChangeMessage message)
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

                foreach (var newMarket in message.Markets.Where(c => c.MarketTemplateId == marketFixture.MarketTemplateId && c.MarketName == marketFixture.MarketName && c.MarketSpecifier == marketFixture.MarketSpecifier))
                {
                    var selectionIds = newMarket.Selections.Select(s => s.SelectionTemplateId).Distinct().ToList();

                    var providerMarketId = $"{Fixture.ProviderFixtureId}:{newMarket.MarketTemplateId}:{newMarket.MarketSpecifier}";
                    var currentMarket = Fixture.Markets.FirstOrDefault(c => c.ProviderMarketId == providerMarketId && c.MarketName == newMarket.MarketName);

                    if (currentMarket != null)
                    {
                        currentMarket.MarketStatusId = newMarket.MarketStatusId;
                        currentMarket.MarketStatusName = ((MarketStatus)newMarket.MarketStatusId).ToString();
                        currentMarket.ClientCode = newMarket.ClientCodes;
                        currentMarket.ClientCodeMargin = newMarket.ClientCodeMargin;

                        foreach (var newSelection in newMarket.Selections)
                        {
                            var providerSelectionId = $"{Fixture.ProviderFixtureId}:{newMarket.MarketTemplateId}:{newSelection.SelectionTemplateId}:{newMarket.MarketSpecifier}";
                            var currentSelection = currentMarket.Selections.FirstOrDefault(c => c.ProviderSelectionId == providerSelectionId);

                            if (currentSelection != null)
                            {
                                if (newSelection.IsActive)
                                {
                                    currentSelection.OldDecimalOdds = currentSelection.DecimalOdds;
                                    currentSelection.DecimalOdds = CommonService.GetFinalOdds(newSelection.Odds).ToString("0.00");
                                    currentSelection.IsActive = newSelection.IsActive;
                                    currentSelection.ClientOdds = newSelection.ClientOdds.Select(s =>
                                        new ClientOddsModel
                                        {
                                            ClientCode = s.ClientCode,
                                            SelectionTemplateId = s.SelectionTemplateId,
                                            Odds = s.DecimalOdds,
                                            OldOdds = currentSelection.ClientOdds != null && currentSelection.ClientOdds.Any(c => c.ClientCode == s.ClientCode) ?
                                                        currentSelection.ClientOdds.Where(c => c.ClientCode == s.ClientCode).FirstOrDefault().Odds : s.DecimalOdds
                                        }).ToList();
                                }
                            }
                        }
                    }
                    else
                    {
                        // add market
                        var market = new MarketModel()
                        {
                            MarketId = newMarket.MarketId.ToString(),
                            ProviderMarketId = providerMarketId,
                            ProviderFixtureId = message.ProviderFixtureId,
                            Line = newMarket.Line,
                            ClientCode = newMarket.ClientCodes,
                            ClientCodeMargin = newMarket.ClientCodeMargin,
                            MarketStatusId = newMarket.MarketStatusId,
                            MarketStatusName = ((MarketStatus)newMarket.MarketStatusId).ToString(),
                            MarketName = newMarket.MarketName,
                            MarketTemplateId = newMarket.MarketTemplateId,
                            Selections = newMarket.Selections.Select(s => new SelectionModel
                            {
                                SelectionId = s.SelectionId.ToString(),
                                ProviderSelectionId = $"{Fixture.ProviderFixtureId}:{newMarket.MarketTemplateId}:{s.SelectionTemplateId}:{newMarket.MarketSpecifier}",
                                OldDecimalOdds = CommonService.GetFinalOdds(s.Odds).ToString("0.00"),
                                DecimalOdds = CommonService.GetFinalOdds(s.Odds).ToString("0.00"),
                                IsActive = s.IsActive,
                                SelectionName = s.SelectionName,
                                SelectionTemplateId = s.SelectionTemplateId,
                                SortOrder = s.SortOrder,
                                ClientOdds = s.ClientOdds.Select(ss =>
                                        new ClientOddsModel
                                        {
                                            ClientCode = ss.ClientCode,
                                            SelectionTemplateId = ss.SelectionTemplateId,
                                            Odds = ss.DecimalOdds,
                                            OldOdds = s.ClientOdds != null && s.ClientOdds.Any(c => c.ClientCode == ss.ClientCode) ?
                                                        s.ClientOdds.Where(c => c.ClientCode == ss.ClientCode).FirstOrDefault().DecimalOdds : ss.DecimalOdds
                                        }).ToList()
                            }).ToList()
                        };

                        Fixture.Markets.Add(market);

                        //var marketTemplateGroupNames = MarketGroupCallback.MarketTemplateGroups.Where(c => c.MarketTemplateGroup.MarketTemplateIds.Contains(market.MarketTemplateId)).Select(s => s.MarketTemplateGroup.MarketTemplateGroupName).Distinct().ToList();
                        //var marketTemplateGroupId = MarketGroupCallback.MarketTemplateGroups.Max(s => s.MarketTemplateGroup.SortOrder) + 1;

                        //foreach (var marketTemplateGroupName in marketTemplateGroupNames)
                        //{
                        //    // validate if market template group is already existing in UI
                        //    if (!MarketGroupCallback.MarketTemplateGroups.Any(c => c.MarketTemplateGroup.MarketTemplateGroupName == marketTemplateGroupName))
                        //    {
                        //        var marketTemplates = new List<string>();
                        //        var rawMarketTemplate = MarketGroupCallback.MarketTemplateGroups.Where(c => c.MarketTemplateGroup.MarketTemplateGroupName == marketTemplateGroupName).Select(s => s.MarketTemplateGroup.MarketTemplateIds).ToList();

                        //        foreach (var lst in rawMarketTemplate)
                        //        {
                        //            marketTemplates.AddRange(lst);
                        //        }

                        //        // add market template group navigation if doesn't exist yet
                        //        MarketGroupCallback.MarketTemplateGroups.Add(new MarketGroupNavigationModel
                        //        {
                        //            IsSelected = MarketGroupCallback.MarketTemplateGroups.Count == 1 ? true : false,
                        //            MarketTemplateGroup = new MarketTemplateGroup
                        //            {
                        //                MarketTemplateGroupId = marketTemplateGroupId,
                        //                MarketTemplateGroupName = MarketGroupCallback.MarketTemplateGroups.FirstOrDefault(s => s.MarketTemplateGroup.MarketTemplateGroupName == marketTemplateGroupName)?.MarketTemplateGroup.MarketTemplateGroupName,
                        //                SortOrder = MarketGroupCallback.MarketTemplateGroups.FirstOrDefault(s => s.MarketTemplateGroup.MarketTemplateGroupName == marketTemplateGroupName).MarketTemplateGroup.SortOrder,
                        //                MarketTemplateIds = marketTemplates.Distinct().ToList()
                        //            }
                        //        });

                        //        marketTemplateGroupId++;
                        //    }
                        //}

                        //// add market template navigation if it doesn't exist yet
                        //foreach (var MarketTemplateGroup in MarketGroupCallback.MarketTemplateGroups)
                        //{
                        //    var groupId = MarketTemplateGroup.MarketTemplateGroup.MarketTemplateGroupId;

                        //    if (MarketTemplateGroup.MarketTemplateGroup.MarketTemplateIds.Contains(market.MarketTemplateId))
                        //    {
                        //        var marketTemplate = MarketTemplates.FirstOrDefault(c => c.MarketTemplateId == market.MarketTemplateId && c.MarketTemplateGroupId == groupId && c.MarketName == market.MarketName);

                        //        // check if market templates is already existing in the list
                        //        if (marketTemplate == null)
                        //        {
                        //            // add market template if not existing
                        //            MarketTemplates.Add(new MarketNavigationModel()
                        //            {
                        //                Id = $"{market.MarketTemplateId}-{groupId}-{market.MarketName.ToLower().Replace(' ', '-')}",
                        //                IsSelected = !MarketTemplates.Any(m => m.MarketTemplateGroupId == groupId && m.IsSelected),
                        //                MarketTemplateGroupId = groupId,
                        //                MarketTemplateId = market.MarketTemplateId,
                        //                MarketName = market.MarketName,
                        //                Fixture = new MarketFixtureResponse.FixtureModel()
                        //                {
                        //                    SportId = fixture.SportId,
                        //                    SportName = fixture.SportName,
                        //                    SegmentId = fixture.SegmentId,
                        //                    SegmentName = fixture.SegmentName,
                        //                    LeagueId = fixture.LeagueId,
                        //                    LeagueName = fixture.LeagueName,
                        //                    ProviderFixtureId = fixture.ProviderFixtureId,
                        //                    FixtureName = fixture.FixtureName,
                        //                    HomeName = fixture.HomeName,
                        //                    AwayName = fixture.AwayName,
                        //                    FixtureStartDate = fixture.FixtureStartDate,
                        //                    IsLive = fixture.IsLive,
                        //                    IsLiveStream = fixture.IsLiveStream,
                        //                    IsSuspended = fixture.IsSuspended,
                        //                    FixtureStatusId = fixture.FixtureStatusId,
                        //                    FixtureStatusName = fixture.FixtureStatusName,
                        //                    SMSCode = fixture.SMSCode,
                        //                    Score = fixture.Score,
                        //                    CurrentPeriod = fixture.CurrentPeriod,
                        //                    CurrentMinute = fixture.CurrentMinute,
                        //                    BetStop = fixture.BetStop,
                        //                    IsBetStopGlobal = fixture.IsBetStopGlobal,
                        //                    Markets = new List<MarketModel>() { market }
                        //                }
                        //            });
                        //        }
                        //        else
                        //        {
                        //            // only add the market in the existing markert template
                        //            marketTemplate.Fixture.Markets.Add(market);
                        //        }
                        //    }
                        //}
                    }
                }
            }

            var selectedNavigation = MarketTemplateNavigation.FirstOrDefault(c => c.IsSelected);

            if (newMarketTemplateIds.Contains(MarketTemplateId) || newMarketTemplateIds.Contains(selectedNavigation.MarketTemplateId))
            {
                if (newMarketTemplateIds.Contains(MarketTemplateId))
                {
                    var markets = Fixture.Markets.Where(c => c.MarketTemplateId == MarketTemplateId).ToList();
                    FixtureMarkets = await PrepareFixtureMarkets(markets);
                }

                if (newMarketTemplateIds.Contains(selectedNavigation.MarketTemplateId))
                {
                    FixtureMarketTemplates = PrepareFixtureMarketTemplates(selectedNavigation.MarketTemplateId);
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task ProcessOnBetStopFixture(OnExternalBetStopFixtureMessage message)
        {
            if (Fixture == null || Id != message.ProviderFixtureId)
                return;

            Fixture.BetStop = message.IsBetStop ? new List<int>() { 1 } : new List<int>();

            FixtureMarkets.ForEach(m =>
            {
                m.BetStop = message.IsBetStop ? new List<int>() { 1 } : new List<int>();
            });

            await InvokeAsync(StateHasChanged);
        }

        private async Task ProcessOnBetStopGlobal(OnExternalBetStopGlobalMessage message)
        {
            //if (MarketTemplates == null)
            //    return;

            Fixture.IsBetStopGlobal = message.IsBetStop;

            FixtureMarkets.ForEach(m =>
            {
                m.IsBetStopGlobal = message.IsBetStop;
            });

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
                OldOdds = clientUnderOdds,
            };
        }

        #endregion

    }
}
