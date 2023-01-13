using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using FixtureModel = Backoffice.Shared.Models.Response.MarketFixtureResponse.FixtureModel;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using BlazorDateRangePicker;
using Backoffice.Shared.Utilities;
using Backoffice.Shared.OutgoingEngineMessages;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.MessageSettlement
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Inject] public IMessageBroker MessageBroker { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Parameter] public EventCallback OnInitializeCallback { get; set; }
        [Parameter] public EventCallback OnAddCallback { get; set; }
        [Parameter] public EventCallback<MarketSettlementFilterModel> MarketSettlementFilterModelChanged { get; set; }
        [Parameter] public MarketSettlementFilterModel MarketSettlementFilterModel { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        public MarketFixtureResponse MarketFixtures { get; set; }
        public MarketFixtureResponse MarketFixtureOutrights { get; set; }
        public bool ShowFilter { get; set; }

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            ShowFilter = false;
            MarketSettlementFilterModel = new MarketSettlementFilterModel()
            {
                Sports = new List<SelectListItemModel<int>>(),
                Segments = new List<SelectListItemModel<int>>(),
                Leagues = new List<SelectListItemModel<int>>(),
                Fixtures = new List<SelectListItemModel<string>>(),
                Markets = new List<SelectListItemModel<string>>(),
                Selections = new List<SelectListItemModel<string>>(),
                Statuses = new List<SelectListItemModel<int>>(),
                FromDate = DateTime.Now.Date,
                ToDate = DateTime.Now.Date.AddDays(1).AddTicks(-1),
            };

            MarketFixtures = new MarketFixtureResponse()
            {
                Fixtures = new List<FixtureModel>(),
            };

            MarketFixtureOutrights = new MarketFixtureResponse()
            {
                Fixtures = new List<FixtureModel>(),
            };
            await Initialize();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                //await Initialize();
            }
        }
        public async Task OnAddMessageSettlement()
        {
            await MarketSettlementFilterModelChanged.InvokeAsync(MarketSettlementFilterModel);

            await OnAddCallback.InvokeAsync();
        }
        public async Task OnFilterClickEvent()
        {
            if (!ShowFilter)
                ShowFilter = true;
            else if (ShowFilter)
                ShowFilter = false;
        }
        public void OnRangeSelect(DateRange range)
        {
            MarketSettlementFilterModel.DateRangeFilter = $"{range.Start.ToString("MM/dd/yyyy")} - {range.End.ToString("MM/dd/yyyy")}";
            MarketSettlementFilterModel.FromDate = Convert.ToDateTime(range.Start.ToString("yyyy-MM-dd 00:00:00"));
            MarketSettlementFilterModel.ToDate = Convert.ToDateTime(range.End.ToString("yyyy-MM-dd 23:59:59"));
        }
        public async Task OnSportChangedEvent(ChangeEventArgs e)
        {
            MarketSettlementFilterModel.SportId = int.Parse(e.Value.ToString());

            MarketSettlementFilterModel.SegmentId = 0;
            MarketSettlementFilterModel.LeagueId = 0;
            MarketSettlementFilterModel.FixtureId = string.Empty;
            MarketSettlementFilterModel.MarketId = string.Empty;
            MarketSettlementFilterModel.SelectionId = string.Empty;

            MarketSettlementFilterModel.Segments = new List<SelectListItemModel<int>>();
            MarketSettlementFilterModel.Leagues = new List<SelectListItemModel<int>>();
            MarketSettlementFilterModel.Fixtures = new List<SelectListItemModel<string>>();
            MarketSettlementFilterModel.Markets = new List<SelectListItemModel<string>>();
            MarketSettlementFilterModel.Selections = new List<SelectListItemModel<string>>();

            if (MarketSettlementFilterModel.SportId != 0)
            {
                MarketSettlementFilterModel.Segments = await GetSegments(MarketSettlementFilterModel.SportId);
                MarketSettlementFilterModel.Leagues = await GetLeagues(MarketSettlementFilterModel.SportId, MarketSettlementFilterModel.SegmentId);
            }
        }
        public async Task OnSegmentChangedEvent(ChangeEventArgs e)
        {
            MarketSettlementFilterModel.SegmentId = int.Parse(e.Value.ToString());

            MarketSettlementFilterModel.LeagueId = 0;
            MarketSettlementFilterModel.FixtureId = string.Empty;
            MarketSettlementFilterModel.MarketId = string.Empty;
            MarketSettlementFilterModel.SelectionId = string.Empty;

            MarketSettlementFilterModel.Leagues = new List<SelectListItemModel<int>>();
            MarketSettlementFilterModel.Fixtures = new List<SelectListItemModel<string>>();
            MarketSettlementFilterModel.Markets = new List<SelectListItemModel<string>>();
            MarketSettlementFilterModel.Selections = new List<SelectListItemModel<string>>();


            if (MarketSettlementFilterModel.SegmentId != 0)
            {
                MarketSettlementFilterModel.Leagues = await GetLeagues(MarketSettlementFilterModel.SportId, MarketSettlementFilterModel.SegmentId);
            }

        }
        public async Task OnLeagueChangedEvent(ChangeEventArgs e)
        {
            MarketSettlementFilterModel.LeagueId = int.Parse(e.Value.ToString());

            MarketSettlementFilterModel.FixtureId = string.Empty;
            MarketSettlementFilterModel.MarketId = string.Empty;
            MarketSettlementFilterModel.SelectionId = string.Empty;

            MarketSettlementFilterModel.Fixtures = new List<SelectListItemModel<string>>();
            MarketSettlementFilterModel.Markets = new List<SelectListItemModel<string>>();
            MarketSettlementFilterModel.Selections = new List<SelectListItemModel<string>>();

            if (MarketSettlementFilterModel.LeagueId != 0)
            {
                MarketSettlementFilterModel.Fixtures = await GetFixtures(MarketSettlementFilterModel.SportId, MarketSettlementFilterModel.SegmentId, MarketSettlementFilterModel.LeagueId);
            }


        }
        public async Task OnFixtureChangedEvent(ChangeEventArgs e)
        {
            MarketSettlementFilterModel.FixtureId = e.Value.ToString();

            MarketSettlementFilterModel.MarketId = string.Empty;
            MarketSettlementFilterModel.SelectionId = string.Empty;

            MarketSettlementFilterModel.Markets = new List<SelectListItemModel<string>>();
            MarketSettlementFilterModel.Selections = new List<SelectListItemModel<string>>();

            if (!string.IsNullOrEmpty(MarketSettlementFilterModel.FixtureId))
            {
                MarketSettlementFilterModel.Markets = await GetMarkets(MarketSettlementFilterModel.FixtureId);
            }

        }
        public async Task OnStatusChangedEvent(ChangeEventArgs e)
        {
            MarketSettlementFilterModel.ToStatus = int.Parse(e.Value.ToString());
        }
        public async Task OnMarketChangedEvent(ChangeEventArgs e)
        {
            MarketSettlementFilterModel.MarketId = e.Value.ToString();

            MarketSettlementFilterModel.SelectionId = string.Empty;

            MarketSettlementFilterModel.Selections = new List<SelectListItemModel<string>>();
            if (!string.IsNullOrEmpty(MarketSettlementFilterModel.MarketId))
            {
                MarketSettlementFilterModel.Selections = await GetSelections(MarketSettlementFilterModel.FixtureId, MarketSettlementFilterModel.MarketId);
            }
        }
        public async Task OnSelectionChangedEvent(ChangeEventArgs e)
        {
            MarketSettlementFilterModel.SelectionId = e.Value.ToString();
        }
        public async Task OnSubmitClickEvent()
        {
            await MarketSettlementFilterModelChanged.InvokeAsync(MarketSettlementFilterModel);

            await OnInitializeCallback.InvokeAsync();
        }
        public async Task OnExportClickEvent()
        {

            if (MarketSettlementFilterModel == null)
                return;

            var payload = new OnGetManualMarketSettlementHistoryMessage
            {
                FixtureId = string.IsNullOrWhiteSpace(MarketSettlementFilterModel.FixtureId) ? "%" : MarketSettlementFilterModel.FixtureId,
                MarketId = string.IsNullOrWhiteSpace(MarketSettlementFilterModel.MarketId) ? "0" : MarketSettlementFilterModel.MarketId,
                SelectionId = string.IsNullOrWhiteSpace(MarketSettlementFilterModel.SelectionId) ? "0" : MarketSettlementFilterModel.SelectionId,
                SportId = MarketSettlementFilterModel.SportId == 0 ? "%" : MarketSettlementFilterModel.SportId.ToString(),
                SegmentId = MarketSettlementFilterModel.SegmentId == 0 ? "%" : MarketSettlementFilterModel.SegmentId.ToString(),
                LeagueId = MarketSettlementFilterModel.LeagueId == 0 ? "%" : MarketSettlementFilterModel.LeagueId.ToString(),
                ManualSettledBy = MarketSettlementFilterModel.SettledBy,
                FromDate = MarketSettlementFilterModel.FromDate.ToUniversalTime(),
                ToDate = MarketSettlementFilterModel.ToDate.ToUniversalTime(),
                UserId = UserModel.Id
            };

            MessageBroker.Publish(JsonSerializer.Serialize(payload), "OnGetManualMessageSettlementHistory", "r.oe.web.config.ongetmanualmessagesettlementhistory", Guid.NewGuid().ToString());
        }
        public void Dispose()
        {
        }

        #endregion
        #region Initialize
        private async Task Initialize()
        {
            try
            {
                var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                MarketSettlementFilterModel = new MarketSettlementFilterModel()
                {
                    Sports = new List<SelectListItemModel<int>>(),
                    Segments = new List<SelectListItemModel<int>>(),
                    Leagues = new List<SelectListItemModel<int>>(),
                    Fixtures = new List<SelectListItemModel<string>>(),
                    Markets = new List<SelectListItemModel<string>>(),
                    Selections = new List<SelectListItemModel<string>>(),
                    Statuses = new List<SelectListItemModel<int>>(),
                    FromDate = DateTime.Now.Date,
                    ToDate = DateTime.Now.Date.AddDays(1).AddTicks(-1),
                    DateRangeFilter = $"{firstDayOfMonth.ToString("MM/dd/yyyy")} - {lastDayOfMonth.ToString("MM/dd/yyyy")}",
                };

                var sports = await GetSports();
                if (sports != null && sports.Any())
                    MarketSettlementFilterModel.Sports.AddRange(sports);
                MarketSettlementFilterModel.SportId = 0;

                var segments = await GetSegments(MarketSettlementFilterModel.SportId);
                if (segments != null && segments.Any())
                    MarketSettlementFilterModel.Segments.AddRange(segments);
                MarketSettlementFilterModel.SegmentId = 0;

                //var leagues = await GetLeagues(MarketSettlementFilterModel.SportId, MarketSettlementFilterModel.SegmentId);
                //if (leagues != null && leagues.Any())
                //    MarketSettlementFilterModel.Leagues.AddRange(segments);
                //MarketSettlementFilterModel.LeagueId = 0;

                //var fixtures = await GetFixtures(MarketSettlementFilterModel.SportId, MarketSettlementFilterModel.SegmentId, MarketSettlementFilterModel.LeagueId);
                //if (fixtures != null && fixtures.Any())
                //    MarketSettlementFilterModel.Fixtures.AddRange(fixtures);
                //MarketSettlementFilterModel.FixtureId = "";

                MarketSettlementFilterModel.FixtureId = string.Empty;
                MarketSettlementFilterModel.MarketId = string.Empty;
                MarketSettlementFilterModel.SelectionId = string.Empty;

                var statuses = await GetStatuses();
                if (statuses != null && statuses.Any())
                    MarketSettlementFilterModel.Statuses.AddRange(statuses);
                MarketSettlementFilterModel.ToStatus = 0;

                await MarketSettlementFilterModelChanged.InvokeAsync(MarketSettlementFilterModel);

                await OnInitializeCallback.InvokeAsync();
            }
            catch (Exception)
            {
            }
        }
        private async Task<List<SelectListItemModel<int>>> GetSports()
        {
            var result = (await TradingService.GetSportLookup()).Result;

            if (result == null)
                return new List<SelectListItemModel<int>>();

            var sports = result.SportLookup?.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SportId,
                Name = s.SportName
            }).ToList();

            return sports;
        }
        private async Task<List<SelectListItemModel<int>>> GetSegments(int sportId)
        {
            var result = (await TradingService.GetSegmentLookup(sportId)).Result;

            var segments = result.SegmentLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SegmentId,
                Name = s.SegmentName
            }).ToList();

            return segments;
        }
        private async Task<List<SelectListItemModel<int>>> GetLeagues(int sportId, int segmentId)
        {
            var result = (await TradingService.GetLeagueLookup(sportId, segmentId)).Result;

            var leagues = result.LeagueLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.LeagueId,
                Name = s.LeagueName
            }).ToList();

            return leagues;
        }
        private async Task<List<SelectListItemModel<string>>> GetFixtures(int sportId, int segmentId, int leagueId)
        {

            MarketFixtures = new MarketFixtureResponse()
            {
                Fixtures = new List<FixtureModel>(),
            };

            MarketFixtureOutrights = new MarketFixtureResponse()
            {
                Fixtures = new List<FixtureModel>(),
            };

            var newMarketFixtureResponse = new List<SelectListItemModel<string>>();

            MarketFixtures = (await TradingService.GetSportsBookFixtures(new MarketFixtureRequest
            {
                FixtureTypeId = (int)FixtureType.Match,
                LeagueId = leagueId,
                SegmentId = segmentId,
                SportId = sportId,
                AllFixtureStatus = true,
            })).Result;

            MarketFixtureOutrights = (await TradingService.GetSportsBookOutrightFixtures(new MarketFixtureRequest
            {
                FixtureTypeId = (int)FixtureType.Outright,
                LeagueId = leagueId,
                SegmentId = segmentId,
                SportId = sportId,
            })).Result;

            if (MarketFixtures == null && MarketFixtureOutrights == null)
                return new List<SelectListItemModel<string>>();

            if (MarketFixtures.Fixtures.Count > 0)
                newMarketFixtureResponse.AddRange(MarketFixtures.Fixtures.Select(s => new SelectListItemModel<string>()
                {
                    Value = s.FixtureId,
                    Name = s.FixtureName
                }).Distinct().ToList());

            if (MarketFixtureOutrights.Fixtures.Count > 0)
                newMarketFixtureResponse.AddRange(MarketFixtureOutrights.Fixtures.Select(s => new SelectListItemModel<string>()
                {
                    Value = s.FixtureId,
                    Name = s.FixtureName
                }).Distinct().ToList());

            return newMarketFixtureResponse.Distinct().ToList();
        }
        private async Task<List<SelectListItemModel<string>>> GetMarkets(string fixtureId)
        {
            if (MarketFixtures == null && MarketFixtureOutrights == null)
                return new List<SelectListItemModel<string>>();

            var markets = new List<SelectListItemModel<string>>();

            if (MarketFixtures != null && MarketFixtures.Fixtures.Any())
            {
                var fixture = MarketFixtures.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
                if (fixture == null)
                    return new List<SelectListItemModel<string>>();

                var markets2 = fixture.Markets?.Select(s => new SelectListItemModel<string>()
                {
                    Value = s.MarketId,
                    Name = GetMarketName(s.MarketName, s.MarketSpecifier)
                }).Distinct().ToList();

                if (markets2 != null && markets2.Any())
                    markets.AddRange(markets2);
            }
            if (MarketFixtureOutrights != null && MarketFixtureOutrights.Fixtures.Any())
            {
                var fixture = MarketFixtureOutrights.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
                if (fixture == null)
                    return new List<SelectListItemModel<string>>();

                var markets2 = fixture.Markets?.Select(s => new SelectListItemModel<string>()
                {
                    Value = s.MarketId,
                    Name = GetMarketName(s.MarketName, s.MarketSpecifier)
                }).Distinct().ToList();

                if (markets2 != null && markets2.Any())
                    markets.AddRange(markets2);
            }

            return markets.Distinct().ToList();
        }
        private async Task<List<SelectListItemModel<string>>> GetSelections(string fixtureId, string marketId)
        {
            if (MarketFixtures == null && MarketFixtureOutrights == null)
                return new List<SelectListItemModel<string>>();

            var selections = new List<SelectListItemModel<string>>();

            if (MarketFixtures != null && MarketFixtures.Fixtures.Any())
            {
                var fixture = MarketFixtures.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
                if (fixture == null)
                    return new List<SelectListItemModel<string>>();

                var market = fixture.Markets.FirstOrDefault(m => m.MarketId == marketId);
                if (market == null)
                    return new List<SelectListItemModel<string>>();

                var selection2 = market.Selections.Select(s => new SelectListItemModel<string>()
                {
                    Value = s.SelectionId,
                    Name = s.SelectionName
                }).Distinct().ToList();

                if (selection2 != null && selection2.Any())
                    selections.AddRange(selection2);
            }
            if (MarketFixtureOutrights != null && MarketFixtureOutrights.Fixtures.Any())
            {
                var fixture = MarketFixtureOutrights.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
                if (fixture == null)
                    return new List<SelectListItemModel<string>>();

                var market = fixture.Markets.FirstOrDefault(m => m.MarketId == marketId);
                if (market == null)
                    return new List<SelectListItemModel<string>>();

                var selection2 = market.Selections.Select(s => new SelectListItemModel<string>()
                {
                    Value = s.SelectionId,
                    Name = s.SelectionName
                }).Distinct().ToList();

                if (selection2 != null && selection2.Any())
                    selections.AddRange(selection2);
            }

            return selections.Distinct().ToList();
        }
        private string GetMarketName(string marketName, string marketSpecifier)
        {
            if (string.IsNullOrEmpty(marketName))
                return string.Empty;

            if (string.IsNullOrEmpty(marketSpecifier))
                return marketName;

            if (!marketSpecifier.Contains("="))
                return marketName;

            var marketLine = marketSpecifier.Split("=")[1];

            if (!marketName.Contains(marketLine) && marketLine != "0")
                return $"{marketName} {marketLine}";
            else
                return marketName;
        }
        private async Task<List<SelectListItemModel<int>>> GetStatuses()
        {
            var statuses = new List<SelectListItemModel<int>>()
            {
                new SelectListItemModel<int>()
                {
                    Value = (int)BetStatus.Won,
                    Name = BetStatus.Won.ToString()
                },
                new SelectListItemModel<int>()
                {
                    Value = (int)BetStatus.Lost,
                    Name = BetStatus.Lost.ToString()
                },
                new SelectListItemModel<int>()
                {
                    Value = (int)BetStatus.Void,
                    Name = BetStatus.Void.ToString()
                },
                new SelectListItemModel<int>()
                {
                    Value = (int)BetStatus.HalfWon,
                    Name = BetStatus.HalfWon.ToString()
                },
                new SelectListItemModel<int>()
                {
                    Value = (int)BetStatus.HalfLost,
                    Name = BetStatus.HalfLost.ToString()
                }
            };

            return statuses;
        }
        #endregion
    }
}
