using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using static Backoffice.Shared.Models.Response.FastOddsFixturesResponse;
using FixtureModel = Backoffice.Shared.Models.Response.MarketFixtureResponse.FixtureModel;

namespace Backoffice.Web.Components.MarketSettlement
{
    public partial class Filter : IDisposable
    {
        #region Properties        
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IMessageBroker MessageBroker { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }

        [Parameter] public MarketSettlementModalFilterModel FilterModel { get; set; }
        [Parameter] public EventCallback<MarketSettlementModalFilterModel> FilterModelChanged { get; set; }
        [Parameter] public EventCallback OnInitializeCallback { get; set; }
        [Parameter] public EventCallback OnExportCallback { get; set; }

        private bool ShowFilter { get; set; }        
        private List<SelectListItemModel<int>> Sports { get; set; }
        private List<SelectListItemModel<int>> Segments { get; set; }
        private List<SelectListItemModel<int>> Leagues { get; set; }
        private List<SelectListItemModel<string>> Fixtures { get; set; }
        private List<SelectListItemModel<string>> Markets { get; set; }
        private List<SelectListItemModel<int>> Statuses { get; set; }
        private List<SelectListItemModel<string>> Selections { get; set; }
        #endregion

        #region Fields
        private MarketFixtureResponse _marketFixtureResponse { get; set; }
        private MarketFixtureResponse _marketFixtureOutrightResponse { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            ShowFilter = false;

            Sports = new List<SelectListItemModel<int>>();
            Segments = new List<SelectListItemModel<int>>();
            Leagues = new List<SelectListItemModel<int>>();
            Fixtures = new List<SelectListItemModel<string>>();
            Markets = new List<SelectListItemModel<string>>();
            Selections = new List<SelectListItemModel<string>>();
            Statuses = new List<SelectListItemModel<int>>();

            FilterModel = new MarketSettlementModalFilterModel()
            {
                FromDate = DateTime.Now.Date.AddDays(-1),
                ToDate = DateTime.Now.Date.AddDays(1).AddTicks(-1),
            };

            _marketFixtureResponse = new MarketFixtureResponse()
            {
                Fixtures = new List<FixtureModel>(),
            };

            _marketFixtureOutrightResponse = new MarketFixtureResponse()
            {
                Fixtures = new List<FixtureModel>(),
            };
            await Initialize();
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

        public void OnRangeSelect(DateRange range)
        {
            FilterModel.FromDate = Convert.ToDateTime(range.Start.ToString("yyyy-MM-dd 00:00:00"));
            FilterModel.ToDate = Convert.ToDateTime(range.End.ToString("yyyy-MM-dd 23:59:59"));
        }

        public async Task OnExportClickEvent()
        {
            if (FilterModel == null)
                return;

            var payload = new OnGetMarketSettlementHistoryMessage
            {
                SegmentId = FilterModel.SegmentId == 0 ? String.Empty : FilterModel.SegmentId.ToString(),
                LeagueId = FilterModel.LeagueId == 0 ? String.Empty : FilterModel.LeagueId.ToString(),
                FixtureId = FilterModel.FixtureId,
                MarketId = FilterModel.MarketId,
                SelectionId = FilterModel.SelectionId,
                SportId = FilterModel.SportId == 0 ? string.Empty : FilterModel.SportId.ToString(),
                SelectionStatus = FilterModel.ToStatus == 0 ? string.Empty : FilterModel.ToStatus.ToString(),
                ManualSettledBy = FilterModel.SettledBy,
                FromDate = FilterModel.FromDate,
                ToDate = FilterModel.ToDate,
                UserId = UserModel.Id,
                ConnectionId = UserModel.ConnectionId
            };

            MessageBroker.Publish(JsonSerializer.Serialize(payload), "ExportMarketSettlementHistory", "r.bo.util.export", Guid.NewGuid().ToString());
            await OnExportCallback.InvokeAsync();
        }
        public async Task OnSearchClickEvent()
        {
            await FilterModelChanged.InvokeAsync(FilterModel);

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
                Sports = new List<SelectListItemModel<int>>();
                Segments = new List<SelectListItemModel<int>>();
                Leagues = new List<SelectListItemModel<int>>();
                Fixtures = new List<SelectListItemModel<string>>();
                Markets = new List<SelectListItemModel<string>>();
                Selections = new List<SelectListItemModel<string>>();
                Statuses = new List<SelectListItemModel<int>>();

                FilterModel = new MarketSettlementModalFilterModel()
                {
                    FromDate = DateTime.Now.Date.AddDays(-1),
                    ToDate = DateTime.Now.Date.AddDays(1).AddTicks(-1),
                };

                var sports = await GetSports();
                if (sports != null && sports.Any())
                    Sports.AddRange(sports);
                FilterModel.SportId = 0;

                var segments = await GetSegments(FilterModel.SportId);
                if (segments != null && segments.Any())
                    Segments.AddRange(segments);
                FilterModel.SegmentId = 0;

                var leagues = await GetLeagues(FilterModel.SportId, FilterModel.SegmentId);
                if (leagues != null && leagues.Any())
                    Leagues.AddRange(segments);
                FilterModel.LeagueId = 0;

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

            _marketFixtureResponse = new MarketFixtureResponse();

            _marketFixtureOutrightResponse = new MarketFixtureResponse();

            var newMarketFixtureResponse = new List<SelectListItemModel<string>>();

            _marketFixtureResponse = (await TradingService.GetSportsBookFixtures(new MarketFixtureRequest
            {
                FixtureTypeId = (int)FixtureType.Match,
                LeagueId = leagueId,
                SegmentId = segmentId,
                SportId = sportId,
                AllFixtureStatus = true,
            })).Result;

            _marketFixtureOutrightResponse = (await TradingService.GetSportsBookOutrightFixtures(new MarketFixtureRequest
            {
                FixtureTypeId = (int)FixtureType.Outright,
                LeagueId = leagueId,
                SegmentId = segmentId,
                SportId = sportId,
            })).Result;

            if (_marketFixtureResponse == null && _marketFixtureOutrightResponse == null)
                return new List<SelectListItemModel<string>>();

            if (_marketFixtureResponse.Fixtures.Count > 0)
                newMarketFixtureResponse.AddRange(_marketFixtureResponse.Fixtures.Select(s => new SelectListItemModel<string>()
                {
                    Value = s.FixtureId,
                    Name = s.FixtureName
                }).Distinct().ToList());

            if (_marketFixtureOutrightResponse.Fixtures.Count > 0)
                newMarketFixtureResponse.AddRange(_marketFixtureOutrightResponse.Fixtures.Select(s => new SelectListItemModel<string>()
                {
                    Value = s.FixtureId,
                    Name = s.FixtureName
                }).Distinct().ToList());

            return newMarketFixtureResponse.Distinct().ToList();
        }

        private async Task<List<SelectListItemModel<string>>> GetMarkets(string fixtureId)
        {
            if (_marketFixtureResponse == null && _marketFixtureOutrightResponse == null)
                return new List<SelectListItemModel<string>>();

            var markets = new List<SelectListItemModel<string>>();

            if (_marketFixtureResponse != null && _marketFixtureResponse.Fixtures.Any())
            {
                var fixture = _marketFixtureResponse.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
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
            if (_marketFixtureOutrightResponse != null && _marketFixtureOutrightResponse.Fixtures.Any())
            {
                var fixture = _marketFixtureOutrightResponse.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
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
            if (_marketFixtureResponse == null && _marketFixtureOutrightResponse == null)
                return new List<SelectListItemModel<string>>();

            var selections = new List<SelectListItemModel<string>>();

            if (_marketFixtureResponse != null && _marketFixtureResponse.Fixtures.Any())
            {
                var fixture = _marketFixtureResponse.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
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
            if (_marketFixtureOutrightResponse != null && _marketFixtureOutrightResponse.Fixtures.Any())
            {
                var fixture = _marketFixtureOutrightResponse.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
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
