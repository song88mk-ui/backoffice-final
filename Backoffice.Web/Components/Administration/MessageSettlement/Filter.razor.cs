using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using Serilog;
using FixtureModel = Backoffice.Shared.Models.Response.MarketFixtureResponse.FixtureModel;

namespace Backoffice.Web.Components.Administration.MessageSettlement
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public ITradingService TradingService { get; set; }

        [Parameter] public EventCallback OnInitializeCallback { get; set; }
        [Parameter] public EventCallback OnAddCallback { get; set; }
        [Parameter] public EventCallback OnExportCallback { get; set; }
        [Parameter] public EventCallback<MarketSettlementFilterModel> FilterModelChanged { get; set; }
        [Parameter] public MarketSettlementFilterModel FilterModel { get; set; }

        public List<SelectListItemModel<int>> Sports { get; set; }
        public List<SelectListItemModel<int>> Segments { get; set; }
        public List<SelectListItemModel<int>> Leagues { get; set; }
        public List<SelectListItemModel<string>> Fixtures { get; set; }
        public List<SelectListItemModel<string>> Markets { get; set; }
        public List<SelectListItemModel<string>> Selections { get; set; }
        public List<SelectListItemModel<int>> Statuses { get; set; }
        public MarketFixtureResponse MarketFixtures { get; set; }
        public MarketFixtureResponse MarketFixtureOutrights { get; set; }

        public bool ShowFilter { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ShowFilter = false;

            MarketFixtures = new MarketFixtureResponse();
            MarketFixtureOutrights = new MarketFixtureResponse();

            await FilterModelChanged.InvokeAsync(FilterModel);
            await Initialize();
        }

        public async Task OnAddMessageSettlement()
        {
            await FilterModelChanged.InvokeAsync(FilterModel);

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
            FilterModel.FromDate = range.Start.DateTime;
            FilterModel.ToDate = range.End.DateTime;
        }

        public async Task OnSportChangedEvent(ChangeEventArgs e)
        {
            FilterModel.SportId = int.Parse(e.Value.ToString());

            FilterModel.SegmentId = 0;
            FilterModel.LeagueId = 0;
            FilterModel.FixtureId = string.Empty;
            FilterModel.MarketId = string.Empty;
            FilterModel.SelectionId = string.Empty;

            Segments = new List<SelectListItemModel<int>>();
            Leagues = new List<SelectListItemModel<int>>();
            Fixtures = new List<SelectListItemModel<string>>();
            Markets = new List<SelectListItemModel<string>>();
            Selections = new List<SelectListItemModel<string>>();

            if (FilterModel.SportId != 0)
            {
                Segments = await GetSegments(FilterModel.SportId);
                Leagues = await GetLeagues(FilterModel.SportId, FilterModel.SegmentId);
            }
        }

        public async Task OnSegmentChangedEvent(ChangeEventArgs e)
        {
            FilterModel.SegmentId = int.Parse(e.Value.ToString());

            FilterModel.LeagueId = 0;
            FilterModel.FixtureId = string.Empty;
            FilterModel.MarketId = string.Empty;
            FilterModel.SelectionId = string.Empty;

            Leagues = new List<SelectListItemModel<int>>();
            Fixtures = new List<SelectListItemModel<string>>();
            Markets = new List<SelectListItemModel<string>>();
            Selections = new List<SelectListItemModel<string>>();


            if (FilterModel.SegmentId != 0)
            {
                Leagues = await GetLeagues(FilterModel.SportId, FilterModel.SegmentId);
            }

        }

        public async Task OnLeagueChangedEvent(ChangeEventArgs e)
        {
            FilterModel.LeagueId = int.Parse(e.Value.ToString());

            FilterModel.FixtureId = string.Empty;
            FilterModel.MarketId = string.Empty;
            FilterModel.SelectionId = string.Empty;

            Fixtures = new List<SelectListItemModel<string>>();
            Markets = new List<SelectListItemModel<string>>();
            Selections = new List<SelectListItemModel<string>>();

            if (FilterModel.LeagueId != 0)
            {
                Fixtures = await GetFixtures(FilterModel.SportId, FilterModel.SegmentId, FilterModel.LeagueId);
            }


        }

        public async Task OnFixtureChangedEvent(ChangeEventArgs e)
        {
            FilterModel.FixtureId = e.Value.ToString();

            FilterModel.MarketId = string.Empty;
            FilterModel.SelectionId = string.Empty;

            Markets = new List<SelectListItemModel<string>>();
            Selections = new List<SelectListItemModel<string>>();

            if (!string.IsNullOrEmpty(FilterModel.FixtureId))
            {
                Markets = await GetMarkets(FilterModel.FixtureId);
            }

        }

        public async Task OnStatusChangedEvent(ChangeEventArgs e)
        {
            FilterModel.ToStatus = int.Parse(e.Value.ToString());
        }

        public async Task OnMarketChangedEvent(ChangeEventArgs e)
        {
            FilterModel.MarketId = e.Value.ToString();

            FilterModel.SelectionId = string.Empty;

            Selections = new List<SelectListItemModel<string>>();
            if (!string.IsNullOrEmpty(FilterModel.MarketId))
            {
                Selections = await GetSelections(FilterModel.FixtureId, FilterModel.MarketId);
            }
        }

        public async Task OnSelectionChangedEvent(ChangeEventArgs e)
        {
            FilterModel.SelectionId = e.Value.ToString();
        }

        public async Task OnSubmitClickEvent()
        {
            await FilterModelChanged.InvokeAsync(FilterModel);

            await OnInitializeCallback.InvokeAsync();
        }

        public async Task OnExportClickEvent()
        {

            if (FilterModel == null)
                return;

            var payload = new OnGetMessageSettlementHistoryMessage()
            {
                UserId = _userState.Id,
                ConnectionId = _userState.ConnectionId,
                SportId = FilterModel.SportId,
                SegmentId = FilterModel.SegmentId,
                LeagueId = FilterModel.LeagueId,
                FixtureId = FilterModel.FixtureId,
                MarketId = FilterModel.MarketId,
                SelectionId = FilterModel.SelectionId,
                Status = FilterModel.ToStatus,
                FromDate = FilterModel.FromDate,
                ToDate = FilterModel.ToDate,
                SettledBy = FilterModel.SettledBy
            };

            // Task - Migrate to api call
            //MessageBroker.Publish(JsonSerializer.Serialize(payload), "ExportMessageSettlementHistory", $"r.bo.util.export", Guid.NewGuid().ToString());

            await OnExportCallback.InvokeAsync();
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
                Sports = new List<SelectListItemModel<int>>();
                Segments = new List<SelectListItemModel<int>>();
                Leagues = new List<SelectListItemModel<int>>();
                Fixtures = new List<SelectListItemModel<string>>();
                Markets = new List<SelectListItemModel<string>>();
                Selections = new List<SelectListItemModel<string>>();
                Statuses = new List<SelectListItemModel<int>>();

                FilterModel = new MarketSettlementFilterModel()
                {
                    FromDate = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddDays(-1),
                    ToDate = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddDays(1).AddTicks(-1),
                };

                var sports = await GetSports();
                if (sports != null && sports.Any())
                    Sports.AddRange(sports);
                FilterModel.SportId = 0;

                var segments = await GetSegments(FilterModel.SportId);
                if (segments != null && segments.Any())
                    Segments.AddRange(segments);
                FilterModel.SegmentId = 0;

                //var leagues = await GetLeagues(MarketSettlementFilterModel.SportId, MarketSettlementFilterModel.SegmentId);
                //if (leagues != null && leagues.Any())
                //    MarketSettlementFilterModel.Leagues.AddRange(segments);
                //MarketSettlementFilterModel.LeagueId = 0;

                //var fixtures = await GetFixtures(MarketSettlementFilterModel.SportId, MarketSettlementFilterModel.SegmentId, MarketSettlementFilterModel.LeagueId);
                //if (fixtures != null && fixtures.Any())
                //    MarketSettlementFilterModel.Fixtures.AddRange(fixtures);
                //MarketSettlementFilterModel.FixtureId = "";

                FilterModel.FixtureId = string.Empty;
                FilterModel.MarketId = string.Empty;
                FilterModel.SelectionId = string.Empty;

                var statuses = await GetStatuses();
                if (statuses != null && statuses.Any())
                    Statuses.AddRange(statuses);
                FilterModel.ToStatus = 0;

                await FilterModelChanged.InvokeAsync(FilterModel);

                await OnInitializeCallback.InvokeAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }
        }

        private async Task<List<SelectListItemModel<int>>> GetSports()
        {
            var result = (await TradingService.GetSportLookup(_translationState.ActiveLanguageCode.ToString().ToUpper())).Result;

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

            var segments = result.SegmentLookup?.Select(s => new SelectListItemModel<int>()
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
