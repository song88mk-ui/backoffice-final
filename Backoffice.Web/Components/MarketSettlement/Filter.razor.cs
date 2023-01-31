using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using FixtureModel = Backoffice.Shared.Models.Response.MarketFixtureResponse.FixtureModel;

namespace Backoffice.Web.Components.MarketSettlement
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [Parameter] public MarketSettlementModalFilterModel ModalFilter { get; set; }
        [Parameter] public EventCallback<MarketSettlementModalFilterModel> ModalFilterChanged { get; set; }
        [Parameter] public EventCallback OnInitializeCallback { get; set; }
        public bool ShowFilter { get; set; }
        public MarketFixtureResponse MarketFixtureResponse { get; set; }
        public MarketFixtureResponse MarketFixtureOutrightResponse { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            ShowFilter = false;

            ModalFilter = new MarketSettlementModalFilterModel()
            {
                Sports = await GetSports(),
                Segments = await GetSegments(0),
                Leagues = await GetLeagues(0, 0),
                Fixtures = await GetFixtures(0, 0, 0),
                Markets = await GetMarkets(""),
                Selections = await GetSelections("" , ""),
                Statuses = new List<SelectListItemModel<int>>(),
                FromDate = DateTime.Now.Date,
                ToDate = DateTime.Now.Date.AddDays(1).AddTicks(-1),
            };
            await ModalFilterChanged.InvokeAsync();

            MarketFixtureResponse = new MarketFixtureResponse()
            {
                Fixtures = new List<FixtureModel>(),
            };

            MarketFixtureOutrightResponse = new MarketFixtureResponse()
            {
                Fixtures = new List<FixtureModel>(),
            };
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await Initialize();
            }
        }

        public async Task OnFilterClickEvent()
        {
            ShowFilter = !ShowFilter;
        }

        public async Task OnAddMarketSettlementClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 100rem");

            var options = new ModalOptions();
            ModalService.Open<AddMarketSettlementModal>($"Add Market Settlement", parameters, options);
        }

        public async Task OnSportChangedEvent(ChangeEventArgs e)
        {
            ModalFilter.SportId = int.Parse(e.Value.ToString());

            ModalFilter.SegmentId = 0;
            ModalFilter.LeagueId = 0;
            ModalFilter.FixtureId = string.Empty;
            ModalFilter.MarketId = string.Empty;
            ModalFilter.SelectionId = string.Empty;

            ModalFilter.Segments = new List<SelectListItemModel<int>>();
            ModalFilter.Leagues = new List<SelectListItemModel<int>>();
            ModalFilter.Fixtures = new List<SelectListItemModel<string>>();
            ModalFilter.Markets = new List<SelectListItemModel<string>>();
            ModalFilter.Selections = new List<SelectListItemModel<string>>();

            if (ModalFilter.SportId != 0)
            {
                ModalFilter.Segments = await GetSegments(ModalFilter.SportId);
                ModalFilter.Leagues = await GetLeagues(ModalFilter.SportId, ModalFilter.SegmentId);
            }
        }

        public async Task OnSegmentChangedEvent(ChangeEventArgs e)
        {
            ModalFilter.SegmentId = int.Parse(e.Value.ToString());

            ModalFilter.LeagueId = 0;
            ModalFilter.FixtureId = string.Empty;
            ModalFilter.MarketId = string.Empty;
            ModalFilter.SelectionId = string.Empty;

            ModalFilter.Leagues = new List<SelectListItemModel<int>>();
            ModalFilter.Fixtures = new List<SelectListItemModel<string>>();
            ModalFilter.Markets = new List<SelectListItemModel<string>>();
            ModalFilter.Selections = new List<SelectListItemModel<string>>();


            if (ModalFilter.SegmentId != 0)
            {
                ModalFilter.Leagues = await GetLeagues(ModalFilter.SportId, ModalFilter.SegmentId);
            }
        }

        public async Task OnLeagueChangedEvent(ChangeEventArgs e)
        {
            ModalFilter.LeagueId = int.Parse(e.Value.ToString());

            ModalFilter.FixtureId = string.Empty;
            ModalFilter.MarketId = string.Empty;
            ModalFilter.SelectionId = string.Empty;

            ModalFilter.Fixtures = new List<SelectListItemModel<string>>();
            ModalFilter.Markets = new List<SelectListItemModel<string>>();
            ModalFilter.Selections = new List<SelectListItemModel<string>>();

            if (ModalFilter.LeagueId != 0)
            {
                ModalFilter.Fixtures = await GetFixtures(ModalFilter.SportId, ModalFilter.SegmentId, ModalFilter.LeagueId);
            }
        }

        public async Task OnFixtureChangedEvent(ChangeEventArgs e)
        {
            ModalFilter.FixtureId = e.Value.ToString();

            ModalFilter.MarketId = string.Empty;
            ModalFilter.SelectionId = string.Empty;

            ModalFilter.Markets = new List<SelectListItemModel<string>>();
            ModalFilter.Selections = new List<SelectListItemModel<string>>();

            if (!string.IsNullOrEmpty(ModalFilter.FixtureId))
            {
                ModalFilter.Markets = await GetMarkets(ModalFilter.FixtureId);
            }
        }

        public async Task OnStatusChangedEvent(ChangeEventArgs e)
        {
            ModalFilter.ToStatus = int.Parse(e.Value.ToString());
        }

        public async Task OnMarketChangedEvent(ChangeEventArgs e)
        {
            ModalFilter.MarketId = e.Value.ToString();

            ModalFilter.SelectionId = string.Empty;

            ModalFilter.Selections = new List<SelectListItemModel<string>>();
            if (!string.IsNullOrEmpty(ModalFilter.MarketId))
            {
                ModalFilter.Selections = await GetSelections(ModalFilter.FixtureId, ModalFilter.MarketId);
            }
        }

        public async Task OnSelectionChangedEvent(ChangeEventArgs e)
        {
            ModalFilter.SelectionId = e.Value.ToString();
        }

        public void OnRangeSelect(DateRange range)
        {
            ModalFilter.DateRangeFilter = $"{range.Start.ToString("MM/dd/yyyy")} - {range.End.ToString("MM/dd/yyyy")}";
            ModalFilter.FromDate = Convert.ToDateTime(range.Start.ToString("yyyy-MM-dd 00:00:00"));
            ModalFilter.ToDate = Convert.ToDateTime(range.End.ToString("yyyy-MM-dd 23:59:59"));
        }

        public async Task OnSearchClickEvent()
        {
            await ModalFilterChanged.InvokeAsync(ModalFilter);

            await OnInitializeCallback.InvokeAsync();
        }

        public void Dispose()
        {

        }
        #endregion

        #region Utilities
        private async Task Initialize()
        {
            try
            {
                var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                ModalFilter = new MarketSettlementModalFilterModel()
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
                    ModalFilter.Sports.AddRange(sports);
                ModalFilter.SportId = 0;

                var segments = await GetSegments(ModalFilter.SportId);
                if (segments != null && segments.Any())
                    ModalFilter.Segments.AddRange(segments);
                ModalFilter.SegmentId = 0;

                var leagues = await GetLeagues(ModalFilter.SportId, ModalFilter.SegmentId);
                if (leagues != null && leagues.Any())
                    ModalFilter.Leagues.AddRange(segments);
                ModalFilter.LeagueId = 0;

                ModalFilter.FixtureId = string.Empty;
                ModalFilter.MarketId = string.Empty;
                ModalFilter.SelectionId = string.Empty;

                var statuses = await GetStatuses();
                if (statuses != null && statuses.Any())
                    ModalFilter.Statuses.AddRange(statuses);
                ModalFilter.ToStatus = 0;

                await ModalFilterChanged.InvokeAsync(ModalFilter);

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

            MarketFixtureResponse = new MarketFixtureResponse();

            MarketFixtureOutrightResponse = new MarketFixtureResponse();

            var newMarketFixtureResponse = new List<SelectListItemModel<string>>();

            MarketFixtureResponse = (await TradingService.GetSportsBookFixtures(new MarketFixtureRequest
            {
                FixtureTypeId = (int)FixtureType.Match,
                LeagueId = leagueId,
                SegmentId = segmentId,
                SportId = sportId,
                AllFixtureStatus = true,
            })).Result;

            MarketFixtureOutrightResponse = (await TradingService.GetSportsBookOutrightFixtures(new MarketFixtureRequest
            {
                FixtureTypeId = (int)FixtureType.Outright,
                LeagueId = leagueId,
                SegmentId = segmentId,
                SportId = sportId,
            })).Result;

            if (MarketFixtureResponse == null && MarketFixtureOutrightResponse == null)
                return new List<SelectListItemModel<string>>();

            if (MarketFixtureResponse.Fixtures.Count > 0)
                newMarketFixtureResponse.AddRange(MarketFixtureResponse.Fixtures.Select(s => new SelectListItemModel<string>()
                {
                    Value = s.FixtureId,
                    Name = s.FixtureName
                }).Distinct().ToList());

            if (MarketFixtureOutrightResponse.Fixtures.Count > 0)
                newMarketFixtureResponse.AddRange(MarketFixtureOutrightResponse.Fixtures.Select(s => new SelectListItemModel<string>()
                {
                    Value = s.FixtureId,
                    Name = s.FixtureName
                }).Distinct().ToList());

            return newMarketFixtureResponse.Distinct().ToList();
        }

        private async Task<List<SelectListItemModel<string>>> GetMarkets(string fixtureId)
        {
            if (MarketFixtureResponse == null && MarketFixtureOutrightResponse == null)
                return new List<SelectListItemModel<string>>();

            var markets = new List<SelectListItemModel<string>>();

            if (MarketFixtureResponse != null && MarketFixtureResponse.Fixtures.Any())
            {
                var fixture = MarketFixtureResponse.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
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
            if (MarketFixtureOutrightResponse != null && MarketFixtureOutrightResponse.Fixtures.Any())
            {
                var fixture = MarketFixtureOutrightResponse.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
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

        private async Task<List<SelectListItemModel<string>>> GetSelections(string fixtureId, string marketId)
        {
            if (MarketFixtureResponse == null && MarketFixtureOutrightResponse == null)
                return new List<SelectListItemModel<string>>();

            var selections = new List<SelectListItemModel<string>>();

            if (MarketFixtureResponse != null && MarketFixtureResponse.Fixtures.Any())
            {
                var fixture = MarketFixtureResponse.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
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
            if (MarketFixtureOutrightResponse != null && MarketFixtureOutrightResponse.Fixtures.Any())
            {
                var fixture = MarketFixtureOutrightResponse.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
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
        #endregion
    }
}
