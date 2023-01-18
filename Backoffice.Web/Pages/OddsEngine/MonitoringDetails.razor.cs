using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;
using static Backoffice.Shared.Models.Response.FastOddsFixturesResponse;
using Backoffice.Web.Components.OddsEngine.MonitoringDetails;
using static Backoffice.Shared.Models.Response.MarketTemplateGroupResponse;
using Microsoft.AspNetCore.SignalR.Client;
using Backoffice.Shared.Enums;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MonitoringDetails : IDisposable
    {
        #region Properties
        [Inject] public ICommonService CommonService { get; set; }
        [Inject] public IConfiguration Configuration { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        [Parameter] public string Id { get; set; }
        private HubConnection HubConnection { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public MonitoringDetailsCallbackModel CallbackModel { get; set; }
        public MarketFilterCallbackModel MarketGroupCallback { get; set; }
        public ClientModel ClientModel { get; set; }
        public List<FixtureMarketModel> FilteredMarkets { get; set; }
        public List<MarketNavigationModel> MarketTemplates { get; set; }
        public List<MarketTemplatePlayerPropsResponse> PlayerPropsMarketTemplates { get; set; }

        #endregion

        #region Fields
        //public List<MarketTemplateGroupResponse.MarketTemplateGroup> fullMarketTemplateGroup;
        public MarketGroupFilter _marketGroupFilter = default!;
        public MarketFilter _marketFilter = default!;
        public MarketFixtureResponse.FixtureModel fixture;
        public MarketModel marginMarket;
        public bool isPlayerProps;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            //MarketTemplateGroups = new List<MarketGroupNavigationModel>();
            MarketTemplates = new List<MarketNavigationModel>();
            PlayerPropsMarketTemplates = new List<MarketTemplatePlayerPropsResponse>();

            CallbackModel = new MonitoringDetailsCallbackModel();
            MarketGroupCallback = new MarketFilterCallbackModel();
            ClientModel = new ClientModel();


            var clients = (await BackOfficeService.GetClients()).Result;

            if (clients != null)
            {
                ClientModel = new ClientModel
                {
                    Clients = UserModel.ClientCodes.Any() ?
                        clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList() : new List<ClientModel.Client>()
                };
            }


            PageTitle = "American Football - USA - NCAA, Regular Season";
            IconClass = "fa fa-trophy";
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    await Subscribe();
                }
                catch (Exception)
                {
                }

            }
        }

        public async Task OnInitEvent()
        {
            fixture = CallbackModel.Fixture.Fixture;
            if (fixture != null)
            {
                FilteredMarkets = GetFixtureMarkets(fixture.Markets.Where(c => c.MarketTemplateId == CallbackModel.SelectedMarketTemplateId).ToList());

                PlayerPropsMarketTemplates = (await BackOfficeService.GetMarketTemplatePlayerProps(new MarketTemplatePlayerPropsRequest
                {
                    SportId = fixture.SportId
                })).Result;

                #region Market Template Group Nagivation
                //MarketTemplateGroups = new List<MarketGroupNavigationModel>();
                await _marketGroupFilter.OnInitMarketTemplateGroup(fixture);

                #endregion


            }

        }

        public async Task MonitoringDetailsCallback(MonitoringDetailsCallbackModel model)
        {
            CallbackModel = model;

            await OnInitEvent();

            PageTitle = $"{model.Fixture.Fixture.SportName} - {model.Fixture.Fixture.SegmentName} - {model.Fixture.Fixture.LeagueName}";
            IconClass = $"iconfont iconfont-{model.Fixture.Fixture.SportName.ToLower().Replace(" ", "-")}";

        }

        public async Task OnInitMarketTemplate(MarketFilterCallbackModel callback)
        {
            MarketGroupCallback = callback;

            if (fixture != null)
            {
                #region Market Template Navigation

                MarketTemplates = new List<MarketNavigationModel>();

                foreach (var MarketTemplateGroup in callback.MarketTemplateGroups)
                {
                    var groupId = MarketTemplateGroup.MarketTemplateGroup.MarketTemplateGroupId;

                    foreach (var market in fixture.Markets)
                    {
                        if (MarketTemplateGroup.MarketTemplateGroup.MarketTemplateIds.Contains(market.MarketTemplateId))
                        {
                            var marketTemplate = MarketTemplates.FirstOrDefault(c => c.MarketTemplateId == market.MarketTemplateId && c.MarketTemplateGroupId == groupId && c.MarketName == market.MarketName);

                            // check if market templates is already existing in the list
                            if (marketTemplate == null)
                            {
                                // add market template if not existing
                                MarketTemplates.Add(new MarketNavigationModel()
                                {
                                    Id = $"{market.MarketTemplateId}-{groupId}-{market.MarketName.ToLower().Replace(' ', '-')}",
                                    IsSelected = !MarketTemplates.Any(m => m.MarketTemplateGroupId == groupId && m.IsSelected),
                                    MarketTemplateGroupId = groupId,
                                    MarketTemplateId = market.MarketTemplateId,
                                    MarketName = market.MarketName,
                                    Fixture = new MarketFixtureResponse.FixtureModel()
                                    {
                                        SportId = fixture.SportId,
                                        SportName = fixture.SportName,
                                        SegmentId = fixture.SegmentId,
                                        SegmentName = fixture.SegmentName,
                                        LeagueId = fixture.LeagueId,
                                        LeagueName = fixture.LeagueName,
                                        ProviderFixtureId = fixture.ProviderFixtureId,
                                        FixtureName = fixture.FixtureName,
                                        HomeName = fixture.HomeName,
                                        AwayName = fixture.AwayName,
                                        FixtureStartDate = fixture.FixtureStartDate,
                                        IsLive = fixture.IsLive,
                                        IsLiveStream = fixture.IsLiveStream,
                                        IsSuspended = fixture.IsSuspended,
                                        FixtureStatusId = fixture.FixtureStatusId,
                                        FixtureStatusName = fixture.FixtureStatusName,
                                        SMSCode = fixture.SMSCode,
                                        Score = fixture.Score,
                                        CurrentPeriod = fixture.CurrentPeriod,
                                        CurrentMinute = fixture.CurrentMinute,
                                        BetStop = fixture.BetStop,
                                        IsBetStopGlobal = fixture.IsBetStopGlobal,
                                        Markets = new List<MarketModel>() { market }
                                    }
                                });
                            }
                            else
                            {
                                // only add the market in the existing markert template
                                marketTemplate.Fixture.Markets.Add(market);
                            }
                        }
                    }
                }

                var marketTemplateGroupId = callback.MarketTemplateGroups?.FirstOrDefault(c => c.IsSelected)?.MarketTemplateGroup.MarketTemplateGroupId ?? 0;

                await _marketFilter.OnInitMarketTemplates(MarketTemplates, marketTemplateGroupId, callback.SearchForm);


                #endregion
            }
        }

        public async Task OnMarketTemplateClicked(List<MarketNavigationModel> marketTemplates)
        {
            MarketTemplates = marketTemplates;

            var marketTemplate = marketTemplates.FirstOrDefault(c => c.IsSelected);

            marginMarket = marketTemplate == null ? new MarketModel() : fixture.Markets.FirstOrDefault(c => c.MarketTemplateId == marketTemplate.MarketTemplateId && c.MarketName == marketTemplate.MarketName) ?? new MarketModel();

            var playerPropMarketTemplates = PlayerPropsMarketTemplates.FirstOrDefault(c => c.FixtureStatusId == 0)?.MarketTemplates ?? string.Empty;
            var marketTemplateIds = playerPropMarketTemplates.Split(",").ToList();

            isPlayerProps = marketTemplateIds.Contains(marginMarket.MarketTemplateId);
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
                await ProcessOddsChangeMonitoring(message);

            });

            HubConnection.On<OnExternalBetStopFixtureMessage>("onbetstopfixture", async (message) =>
            {
                await ProcessOnBetStopFixture(message);

            });

            HubConnection.On<OnExternalBetStopGlobalMessage>("onbetstopglobal", async (message) =>
            {
                await ProcessOnBetStopGlobal(message);

            });

            await HubConnection.StartAsync();
        }

        private async Task ProcessOddsChangeMonitoring(OnExternalOddsChangeMessage message)
        {
            if (fixture == null || fixture.FixtureTypeId != message.FixtureTypeId || Id != message.ProviderFixtureId)
                return;

            if (MarketGroupCallback == null || MarketGroupCallback.MarketTemplateGroups == null || !MarketGroupCallback.MarketTemplateGroups.Any())
                return;

            var newMarketTemplateIds = message.Markets.Select(s => s.MarketTemplateId).ToList();
            var marketFixtures = fixture.Markets.Where(c => newMarketTemplateIds.Contains(c.MarketTemplateId)).ToList();

            if (marketFixtures != null && marketFixtures.Any())
            {
                foreach (var marketFixture in marketFixtures)
                {
                    fixture.BetStop = new List<int>();
                    fixture.CurrentPeriod = message.CurrentPeriod;
                    fixture.CurrentMinute = message.CurrentMinute.ToString();
                    fixture.Score = message.Score;
                    fixture.FixtureStatusId = message.FixtureStatusId;

                    foreach (var newMarket in message.Markets.Where(c => c.MarketTemplateId == marketFixture.MarketTemplateId && c.MarketName == marketFixture.MarketName && c.MarketSpecifier == marketFixture.MarketSpecifier))
                    {
                        var selectionIds = newMarket.Selections.Select(s => s.SelectionTemplateId).Distinct().ToList();

                        var providerMarketId = $"{fixture.ProviderFixtureId}:{newMarket.MarketTemplateId}:{newMarket.MarketSpecifier}";
                        var currentMarket = fixture.Markets.FirstOrDefault(c => c.ProviderMarketId == providerMarketId && c.MarketName == newMarket.MarketName);

                        if (currentMarket != null)
                        {
                            currentMarket.MarketStatusId = newMarket.MarketStatusId;
                            currentMarket.MarketStatusName = ((MarketStatus)newMarket.MarketStatusId).ToString();
                            currentMarket.ClientCode = newMarket.ClientCodes;
                            currentMarket.ClientCodeMargin = newMarket.ClientCodeMargin;

                            foreach (var newSelection in newMarket.Selections)
                            {
                                var providerSelectionId = $"{fixture.ProviderFixtureId}:{newMarket.MarketTemplateId}:{newSelection.SelectionTemplateId}:{newMarket.MarketSpecifier}";
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
                                    ProviderSelectionId = $"{fixture.ProviderFixtureId}:{newMarket.MarketTemplateId}:{s.SelectionTemplateId}:{newMarket.MarketSpecifier}",
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

                            fixture.Markets.Add(market);

                            var marketTemplateGroupNames = MarketGroupCallback.MarketTemplateGroups.Where(c => c.MarketTemplateGroup.MarketTemplateIds.Contains(market.MarketTemplateId)).Select(s => s.MarketTemplateGroup.MarketTemplateGroupName).Distinct().ToList();
                            var marketTemplateGroupId = MarketGroupCallback.MarketTemplateGroups.Max(s => s.MarketTemplateGroup.SortOrder) + 1;

                            foreach (var marketTemplateGroupName in marketTemplateGroupNames)
                            {
                                // validate if market template group is already existing in UI
                                if (!MarketGroupCallback.MarketTemplateGroups.Any(c => c.MarketTemplateGroup.MarketTemplateGroupName == marketTemplateGroupName))
                                {
                                    var marketTemplates = new List<string>();
                                    var rawMarketTemplate = MarketGroupCallback.MarketTemplateGroups.Where(c => c.MarketTemplateGroup.MarketTemplateGroupName == marketTemplateGroupName).Select(s => s.MarketTemplateGroup.MarketTemplateIds).ToList();

                                    foreach (var lst in rawMarketTemplate)
                                    {
                                        marketTemplates.AddRange(lst);
                                    }

                                    // add market template group navigation if doesn't exist yet
                                    MarketGroupCallback.MarketTemplateGroups.Add(new MarketGroupNavigationModel
                                    {
                                        IsSelected = MarketGroupCallback.MarketTemplateGroups.Count == 1 ? true : false,
                                        MarketTemplateGroup = new MarketTemplateGroup
                                        {
                                            MarketTemplateGroupId = marketTemplateGroupId,
                                            MarketTemplateGroupName = MarketGroupCallback.MarketTemplateGroups.FirstOrDefault(s => s.MarketTemplateGroup.MarketTemplateGroupName == marketTemplateGroupName)?.MarketTemplateGroup.MarketTemplateGroupName,
                                            SortOrder = MarketGroupCallback.MarketTemplateGroups.FirstOrDefault(s => s.MarketTemplateGroup.MarketTemplateGroupName == marketTemplateGroupName).MarketTemplateGroup.SortOrder,
                                            MarketTemplateIds = marketTemplates.Distinct().ToList()
                                        }
                                    });

                                    marketTemplateGroupId++;
                                }
                            }

                            // add market template navigation if it doesn't exist yet
                            foreach (var MarketTemplateGroup in MarketGroupCallback.MarketTemplateGroups)
                            {
                                var groupId = MarketTemplateGroup.MarketTemplateGroup.MarketTemplateGroupId;

                                if (MarketTemplateGroup.MarketTemplateGroup.MarketTemplateIds.Contains(market.MarketTemplateId))
                                {
                                    var marketTemplate = MarketTemplates.FirstOrDefault(c => c.MarketTemplateId == market.MarketTemplateId && c.MarketTemplateGroupId == groupId && c.MarketName == market.MarketName);

                                    // check if market templates is already existing in the list
                                    if (marketTemplate == null)
                                    {
                                        // add market template if not existing
                                        MarketTemplates.Add(new MarketNavigationModel()
                                        {
                                            Id = $"{market.MarketTemplateId}-{groupId}-{market.MarketName.ToLower().Replace(' ', '-')}",
                                            IsSelected = !MarketTemplates.Any(m => m.MarketTemplateGroupId == groupId && m.IsSelected),
                                            MarketTemplateGroupId = groupId,
                                            MarketTemplateId = market.MarketTemplateId,
                                            MarketName = market.MarketName,
                                            Fixture = new MarketFixtureResponse.FixtureModel()
                                            {
                                                SportId = fixture.SportId,
                                                SportName = fixture.SportName,
                                                SegmentId = fixture.SegmentId,
                                                SegmentName = fixture.SegmentName,
                                                LeagueId = fixture.LeagueId,
                                                LeagueName = fixture.LeagueName,
                                                ProviderFixtureId = fixture.ProviderFixtureId,
                                                FixtureName = fixture.FixtureName,
                                                HomeName = fixture.HomeName,
                                                AwayName = fixture.AwayName,
                                                FixtureStartDate = fixture.FixtureStartDate,
                                                IsLive = fixture.IsLive,
                                                IsLiveStream = fixture.IsLiveStream,
                                                IsSuspended = fixture.IsSuspended,
                                                FixtureStatusId = fixture.FixtureStatusId,
                                                FixtureStatusName = fixture.FixtureStatusName,
                                                SMSCode = fixture.SMSCode,
                                                Score = fixture.Score,
                                                CurrentPeriod = fixture.CurrentPeriod,
                                                CurrentMinute = fixture.CurrentMinute,
                                                BetStop = fixture.BetStop,
                                                IsBetStopGlobal = fixture.IsBetStopGlobal,
                                                Markets = new List<MarketModel>() { market }
                                            }
                                        });
                                    }
                                    else
                                    {
                                        // only add the market in the existing markert template
                                        marketTemplate.Fixture.Markets.Add(market);
                                    }
                                }
                            }
                        }
                    }
                }

                await InvokeAsync(StateHasChanged);
            }

        }

        private async Task ProcessOnBetStopFixture(OnExternalBetStopFixtureMessage message)
        {
            if (fixture == null || Id != message.ProviderFixtureId)
                return;

            fixture.BetStop = message.IsBetStop ? new List<int>() { 1 } : new List<int>();
            FilteredMarkets.ForEach(m =>
            {
                m.BetStop = message.IsBetStop ? new List<int>() { 1 } : new List<int>();
            });

            await InvokeAsync(StateHasChanged);
        }

        private async Task ProcessOnBetStopGlobal(OnExternalBetStopGlobalMessage message)
        {
            if (MarketTemplates == null)
                return;

            fixture.IsBetStopGlobal = message.IsBetStop;
            FilteredMarkets.ForEach(m =>
            {
                m.IsBetStopGlobal = message.IsBetStop;
            });

            await InvokeAsync(StateHasChanged);
        }

        private List<FixtureMarketModel> GetFixtureMarkets(List<MarketModel> markets)
        {
            var fixture = CallbackModel.Fixture.Fixture;

            var fixtureMarkets = new List<FixtureMarketModel>();
            foreach (var market in markets)
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
            return fixtureMarkets;
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
