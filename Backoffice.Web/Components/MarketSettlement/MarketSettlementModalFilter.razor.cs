using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.MarketSettlement
{
    public partial class MarketSettlementModalFilter : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [Parameter] public EventCallback<AddMarketSettlementModel> OnUpdateRowCallback { get; set; }
        [Parameter] public EventCallback<AddMarketSettlementModel> AddMarketSettlementChanged { get; set; }
        [Parameter] public AddMarketSettlementModel AddMarketSettlement { get; set; }
        [Parameter] public EventCallback<Tuple<MarketFixtureResponse, MarketFixtureResponse>> OnLeagueChangedCallback { get; set; }
        public MarketFixtureResponse MarketFixtures { get; set; }
        public MarketFixtureResponse MarketOutrightFixtures { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            AddMarketSettlement = new AddMarketSettlementModel();
            AddMarketSettlement.AddMarketSettlementSelections.Add(new AddMarketSettlementSelectionModel()
            {
                RowCount = AddMarketSettlement.AddMarketSettlementSelections.Count + 1,
                FixtureId = string.Empty,
                MarketId = string.Empty,
                SelectionId = string.Empty,
                Statuses = await GetStatuses()
            });

            AddMarketSettlement.SportId = 0;
            var sports = await GetSports();
            if (sports != null && sports.Any())
            {
                AddMarketSettlement.Sports.AddRange(sports);
            }

            AddMarketSettlement.SegmentId = 0;
            AddMarketSettlement.LeagueId = 0;
            AddMarketSettlement.FixtureId = string.Empty;
            AddMarketSettlement.MarketId = string.Empty;
        }

        public async Task OnSportChangedEvent(ChangeEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Value.ToString()))
                AddMarketSettlement.SportId = 0;
            else
                AddMarketSettlement.SportId = int.Parse(e.Value.ToString());
            AddMarketSettlement.SegmentId = 0;
            AddMarketSettlement.LeagueId = 0;
            AddMarketSettlement.FixtureId = string.Empty;
            AddMarketSettlement.MarketId = string.Empty;

            AddMarketSettlement.Segments = new List<SelectListItemModel<int>>();
            AddMarketSettlement.Leagues = new List<SelectListItemModel<int>>();
            AddMarketSettlement.Fixtures = new List<SelectListItemModel<string>>();
            AddMarketSettlement.Markets = new List<SelectListItemModel<string>>();

            if (AddMarketSettlement.SportId > 0)
            {                
                AddMarketSettlement.Segments = await GetSegments(AddMarketSettlement.SportId);
            }

            await OnUpdateRowCallback.InvokeAsync(AddMarketSettlement);
        }

        public async Task OnSegmentChangedEvent(ChangeEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Value.ToString()))
                AddMarketSettlement.SegmentId = 0;
            else
                AddMarketSettlement.SegmentId = int.Parse(e.Value.ToString());
            AddMarketSettlement.LeagueId = 0;
            AddMarketSettlement.FixtureId = string.Empty;
            AddMarketSettlement.MarketId = string.Empty;
            AddMarketSettlement.Leagues = new List<SelectListItemModel<int>>();
            AddMarketSettlement.Fixtures = new List<SelectListItemModel<string>>();
            AddMarketSettlement.Markets = new List<SelectListItemModel<string>>();

            if (AddMarketSettlement.SegmentId > 0)
            {
                AddMarketSettlement.Leagues = await GetLeagues(AddMarketSettlement.SportId, AddMarketSettlement.SegmentId);
            }

            await OnUpdateRowCallback.InvokeAsync(AddMarketSettlement);
        }

        public async Task OnLeagueChangedEvent(ChangeEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Value.ToString()))
                AddMarketSettlement.LeagueId = 0;
            else
                AddMarketSettlement.LeagueId = int.Parse(e.Value.ToString());

            AddMarketSettlement.FixtureId = string.Empty;
            AddMarketSettlement.MarketId = string.Empty;
            AddMarketSettlement.Fixtures = new List<SelectListItemModel<string>>();
            AddMarketSettlement.Markets = new List<SelectListItemModel<string>>();

            if (AddMarketSettlement.LeagueId > 0)
            {
                AddMarketSettlement.Fixtures = await GetFixtures(AddMarketSettlement.SportId, AddMarketSettlement.SegmentId, AddMarketSettlement.LeagueId);                
            }

            await OnLeagueChangedCallback.InvokeAsync(new Tuple<MarketFixtureResponse, MarketFixtureResponse>(MarketFixtures, MarketOutrightFixtures));
            await OnUpdateRowCallback.InvokeAsync(AddMarketSettlement);            
        }

        public async Task OnFixtureChangedEvent(ChangeEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Value.ToString()))
                AddMarketSettlement.FixtureId = string.Empty;
            else
                AddMarketSettlement.FixtureId = e.Value.ToString();

            AddMarketSettlement.MarketId = string.Empty;
            AddMarketSettlement.Markets = new List<SelectListItemModel<string>>();

            if (!string.IsNullOrEmpty(AddMarketSettlement.FixtureId))
                AddMarketSettlement.Markets = await GetMarkets(AddMarketSettlement.FixtureId);

            await OnUpdateRowCallback.InvokeAsync(AddMarketSettlement);
        }

        public async Task OnMarketChangedEvent(ChangeEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Value.ToString()))
                AddMarketSettlement.MarketId = string.Empty;
            else
                AddMarketSettlement.MarketId = e.Value.ToString();

            await OnUpdateRowCallback.InvokeAsync(AddMarketSettlement);
        }

        public async Task OnBetIdChangedEvent(ChangeEventArgs e)
        {
            await OnUpdateRowCallback.InvokeAsync(AddMarketSettlement);
        }

        public void Dispose()
        {
            
        }
        #endregion

        #region Utilities
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
        private async Task<List<SelectListItemModel<string>>> GetSelections(string fixtureId, string marketId)
        {
            if (MarketFixtures == null && MarketOutrightFixtures == null)
                return new List<SelectListItemModel<string>>();

            var selections = new List<SelectListItemModel<string>>();

            if (MarketFixtures != null && MarketFixtures.Fixtures.Any())
            {
                var fixture = MarketFixtures.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
                if (fixture != null)
                {
                    var market = fixture.Markets.FirstOrDefault(m => m.MarketId == marketId);
                    if (market != null)
                    {
                        var selection2 = market.Selections.Select(s => new SelectListItemModel<string>()
                        {
                            Value = s.SelectionId,
                            Name = s.SelectionName
                        }).Distinct().ToList();

                        if (selection2 != null && selection2.Any())
                            selections.AddRange(selection2);
                    }
                }
            }
            if (MarketOutrightFixtures != null && MarketOutrightFixtures.Fixtures.Any())
            {
                var fixture = MarketOutrightFixtures.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
                if (fixture != null)
                {
                    var market = fixture.Markets.FirstOrDefault(m => m.MarketId == marketId);
                    if (market != null)
                    {
                        var selection2 = market.Selections.Select(s => new SelectListItemModel<string>()
                        {
                            Value = s.SelectionId,
                            Name = s.SelectionName
                        }).Distinct().ToList();

                        if (selection2 != null && selection2.Any())
                            selections.AddRange(selection2);
                    }
                }
            }

            return selections.Distinct().ToList();
        }

        private async Task<List<SelectListItemModel<string>>> GetFixtures(int sportId, int segmentId, int leagueId)
        {

            MarketFixtures = new MarketFixtureResponse();

            MarketOutrightFixtures = new MarketFixtureResponse();

            var newMarketFixtureResponse = new List<SelectListItemModel<string>>();

            MarketFixtures = (await TradingService.GetSportsBookFixtures(new MarketFixtureRequest
            {
                FixtureTypeId = (int)FixtureType.Match,
                LeagueId = leagueId,
                SegmentId = segmentId,
                SportId = sportId,
                AllFixtureStatus = true,
            })).Result;

            MarketOutrightFixtures = (await TradingService.GetSportsBookOutrightFixtures(new MarketFixtureRequest
            {
                FixtureTypeId = (int)FixtureType.Outright,
                LeagueId = leagueId,
                SegmentId = segmentId,
                SportId = sportId,
            })).Result;

            if (MarketFixtures == null && MarketOutrightFixtures == null)
                return new List<SelectListItemModel<string>>();

            if (MarketFixtures.Fixtures.Count > 0)
                newMarketFixtureResponse.AddRange(MarketFixtures.Fixtures.Select(s => new SelectListItemModel<string>()
                {
                    Value = s.FixtureId,
                    Name = s.FixtureName
                }).Distinct().ToList());

            if (MarketOutrightFixtures.Fixtures.Count > 0)
                newMarketFixtureResponse.AddRange(MarketOutrightFixtures.Fixtures.Select(s => new SelectListItemModel<string>()
                {
                    Value = s.FixtureId,
                    Name = s.FixtureName
                }).Distinct().ToList());

            return newMarketFixtureResponse.Distinct().ToList();
        }

        private async Task<List<SelectListItemModel<string>>> GetMarkets(string fixtureId)
        {
            if (MarketFixtures == null && MarketOutrightFixtures == null)
                return new List<SelectListItemModel<string>>();

            var markets = new List<SelectListItemModel<string>>();

            if (MarketFixtures != null && MarketFixtures.Fixtures.Any())
            {
                var fixture = MarketFixtures.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
                if (fixture != null)
                {
                    var markets2 = fixture.Markets?.Select(s => new SelectListItemModel<string>()
                    {
                        Value = s.MarketId,
                        Name = GetMarketName(s.MarketName, s.MarketSpecifier)
                    }).Distinct().ToList();

                    if (markets2 != null && markets2.Any())
                        markets.AddRange(markets2);
                }
            }
            if (MarketOutrightFixtures != null && MarketOutrightFixtures.Fixtures.Any())
            {
                var fixture = MarketOutrightFixtures.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
                if (fixture != null)
                {
                    var markets2 = fixture.Markets?.Select(s => new SelectListItemModel<string>()
                    {
                        Value = s.MarketId,
                        Name = GetMarketName(s.MarketName, s.MarketSpecifier)
                    }).Distinct().ToList();

                    if (markets2 != null && markets2.Any())
                        markets.AddRange(markets2);
                }
            }

            return markets.Distinct().ToList();
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

        protected string GetMarketLine(string marketName, string line)
        {
            if (!string.IsNullOrEmpty(marketName))
            {
                if (string.IsNullOrEmpty(line))
                {
                    return string.Empty;
                }
                else
                {
                    if (marketName.ToUpper().Contains("TOTAL"))
                        return $"o {decimal.Parse(line)} u";

                    if (marketName.ToUpper().Contains("HANDICAP"))
                        return $"h {decimal.Parse(line)} a";
                }
            }
            return string.Empty;
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
