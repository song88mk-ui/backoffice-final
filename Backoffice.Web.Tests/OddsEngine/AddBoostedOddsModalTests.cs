using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.OddsEngine.ManageBoost;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Backoffice.Web.Tests.OddsEngine
{
    // Component tests for the Add Boosted Odds dialog of Manage Boost (BO-42). The sport, segment and league
    // dropdowns come from the Mongo-backed lookups of the BO API and the fixtures, markets and selections from
    // the Trading API fixture list. Both services are mocks, so these cover the dialog's own cascade.
    public class AddBoostedOddsModalTests : BunitTestContext
    {
        private const int Football = 10;
        private const int Basketball = 4;

        private readonly Mock<IBackofficeService> _backofficeService = new();
        private readonly Mock<ITradingService> _tradingService = new();
        private readonly Mock<IModalService> _modalService = new();
        private readonly List<MarketFixtureRequest> _fixtureRequests = new();
        private readonly List<PriceBoostConfigRequest> _boostRequests = new();
        private readonly List<ModalParameters> _opened = new();

        public AddBoostedOddsModalTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());

            var userState = new UserState();
            userState.Model.Id = "test-user";
            userState.Model.Name = "Test User";
            userState.SetTradingClients(new List<PlatformClientResponse>
            {
                new PlatformClientResponse { ClientId = Guid.NewGuid(), ClientCode = "QTE-A", ClientName = "Client A" }
            });

            _backofficeService.Setup(s => s.GetSportLookup(It.IsAny<string>())).ReturnsAsync(Reply(new SportsLookupResponse
            {
                SportLookup = new List<SportLookupModel>
                {
                    new SportLookupModel { SportId = Football, SportName = "Football" },
                    new SportLookupModel { SportId = Basketball, SportName = "Basketball" }
                }
            }));

            _backofficeService
                .Setup(s => s.GetSegmentLookup(It.IsAny<int>(), It.IsAny<LanguageCode>()))
                .ReturnsAsync((int sportId, LanguageCode _) => Reply(new SegmentLookupResponse
                {
                    SegmentLookup = sportId != Football
                        ? new List<SegmentLookupModel>()
                        : new List<SegmentLookupModel>
                        {
                            new SegmentLookupModel { SportId = Football, SegmentId = 1, SegmentName = "England" },
                            new SegmentLookupModel { SportId = Football, SegmentId = 2, SegmentName = "Spain" }
                        }
                }));

            _backofficeService
                .Setup(s => s.GetLeagueLookup(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<LanguageCode>()))
                .ReturnsAsync((int sportId, int segmentId, LanguageCode _) => Reply(new LeagueLookupResponse
                {
                    LeagueLookup = LeaguesOf(sportId, segmentId)
                }));

            _tradingService
                .Setup(s => s.GetSportsBookFixtures(It.IsAny<MarketFixtureRequest>()))
                .Callback<MarketFixtureRequest>(r => _fixtureRequests.Add(r))
                .ReturnsAsync((MarketFixtureRequest r) => Reply(new MarketFixtureResponse { Fixtures = FixturesOf(r.LeagueId) }));

            _tradingService
                .Setup(s => s.GetPriceBoostConfig(It.IsAny<PriceBoostConfigRequest>()))
                .Callback<PriceBoostConfigRequest>(r => _boostRequests.Add(r))
                .ReturnsAsync(Reply(new List<PriceBoostConfigModel>()));

            _modalService
                .Setup(m => m.Open<NextBoostModalBody>(It.IsAny<string>(), It.IsAny<ModalParameters>(), It.IsAny<ModalOptions>()))
                .Callback<string, ModalParameters, ModalOptions>((_, p, _) => _opened.Add(p));

            Services.AddSingleton<IConfiguration>(configuration);
            Services.AddSingleton(_modalService.Object);
            Services.AddSingleton(new Mock<IPreRenderService>().Object);
            Services.AddSingleton(_tradingService.Object);
            Services.AddSingleton(new Mock<IProviderMappingService>().Object);
            Services.AddSingleton(_backofficeService.Object);
            Services.AddSingleton(new Mock<IAuthenticationService>().Object);
            Services.AddSingleton(new Mock<ITableauService>().Object);
            Services.AddSingleton(new Mock<ICommonService>().Object);
            Services.AddSingleton(new TranslationState(translationService.Object));
            Services.AddSingleton(userState);
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                new Mock<IAuthenticationService>().Object,
                sp.GetRequiredService<NavigationManager>()));
        }

        private static BaseResponse<T> Reply<T>(T result) =>
            new BaseResponse<T> { StatusCode = StatusCode.Success, Message = "Success", Result = result };

        private static List<LeagueLookupResponse.LeagueLookupModel> LeaguesOf(int sportId, int segmentId)
        {
            LeagueLookupResponse.LeagueLookupModel League(int id, string name) =>
                new LeagueLookupResponse.LeagueLookupModel { SportId = sportId, SegmentId = segmentId, LeagueId = id, LeagueName = name };

            return (sportId, segmentId) switch
            {
                (Football, 1) => new() { League(100, "Premier League"), League(101, "Championship") },
                (Football, 2) => new() { League(200, "La Liga") },
                _ => new()
            };
        }

        // A pre-match fixture with one market (Match Winner) holding two selections.
        private static MarketFixtureResponse.FixtureModel Fixture(string id, string name, int leagueId) =>
            new MarketFixtureResponse.FixtureModel
            {
                ProviderFixtureId = id,
                FixtureName = name,
                LeagueId = leagueId,
                FixtureStatusId = 0,
                Markets = new List<MarketFixtureResponse.MarketModel>
                {
                    new MarketFixtureResponse.MarketModel
                    {
                        ProviderFixtureId = id,
                        MarketTemplateId = "1",
                        MarketSpecifier = string.Empty,
                        MarketName = "Match Winner",
                        Selections = new List<MarketFixtureResponse.SelectionModel>
                        {
                            new MarketFixtureResponse.SelectionModel { SelectionTemplateId = "1", SelectionSpecifier = string.Empty, SelectionName = "Home" },
                            new MarketFixtureResponse.SelectionModel { SelectionTemplateId = "2", SelectionSpecifier = string.Empty, SelectionName = "Away" }
                        }
                    }
                }
            };

        private static List<MarketFixtureResponse.FixtureModel> FixturesOf(int leagueId) => leagueId switch
        {
            100 => new() { Fixture("F100", "Arsenal vs Chelsea", 100) },
            200 => new() { Fixture("F200", "Sevilla vs Betis", 200) },
            _ => new()
        };

        private IRenderedComponent<AddBoostedOddsModal> Render()
        {
            var parameters = new ModalParameters();
            parameters.Add("Region", "hk");

            return Render<AddBoostedOddsModal>(ps => ps.AddCascadingValue(parameters));
        }

        private static List<string> Options(IRenderedComponent<AddBoostedOddsModal> cut, string selectId) =>
            cut.FindAll($"#{selectId} option").Select(o => o.TextContent.Trim()).ToList();

        private static string? SelectedValue(IRenderedComponent<AddBoostedOddsModal> cut, string selectId) =>
            cut.FindAll($"#{selectId} option").FirstOrDefault(o => o.HasAttribute("selected"))?.GetAttribute("value");

        private static void Choose(IRenderedComponent<AddBoostedOddsModal> cut, string selectId, string value) =>
            cut.Find($"#{selectId}").Change(value);

        private static void ClickNext(IRenderedComponent<AddBoostedOddsModal> cut) =>
            cut.Find(".modal-save").Click();

        private IRenderedComponent<AddBoostedOddsModal> RenderWithFootballChosen()
        {
            var cut = Render();
            cut.WaitForAssertion(() => Assert.Equal(3, Options(cut, "sportSelect").Count));

            Choose(cut, "sportSelect", Football.ToString());
            cut.WaitForAssertion(() => Assert.Equal("100", SelectedValue(cut, "LeagueSelect")));

            return cut;
        }

        [Fact]
        public void Opens_ListingTheSportsOfTheLookup()
        {
            var cut = Render();

            cut.WaitForAssertion(() => Assert.Equal(new[] { "Select Sport", "Football", "Basketball" }, Options(cut, "sportSelect")));
            Assert.Equal(new[] { "Select Segment" }, Options(cut, "segmentSelect"));
            Assert.Equal(new[] { "Select League" }, Options(cut, "LeagueSelect"));
        }

        [Fact]
        public void ChoosingASport_LoadsItsSegmentsAndLeaguesFromTheLookups_AndSelectsTheFirstOfEach()
        {
            var cut = RenderWithFootballChosen();

            Assert.Equal(new[] { "Select Segment", "England", "Spain" }, Options(cut, "segmentSelect"));
            Assert.Equal("1", SelectedValue(cut, "segmentSelect"));
            Assert.Equal(new[] { "Select League", "Premier League", "Championship" }, Options(cut, "LeagueSelect"));
            Assert.Equal("100", SelectedValue(cut, "LeagueSelect"));

            _backofficeService.Verify(s => s.GetSegmentLookup(Football, It.IsAny<LanguageCode>()), Times.Once);
            _backofficeService.Verify(s => s.GetLeagueLookup(Football, 1, It.IsAny<LanguageCode>()), Times.Once);
        }

        [Fact]
        public void ChoosingASport_LoadsTheFixturesMarketsAndSelectionsOfTheFirstLeague()
        {
            var cut = RenderWithFootballChosen();

            var request = Assert.Single(_fixtureRequests);
            Assert.Equal((Football, 1, 100, (int)FixtureType.Match), (request.SportId, request.SegmentId, request.LeagueId, request.FixtureTypeId));
            Assert.Equal(new[] { "Select Fixture", "Arsenal vs Chelsea" }, Options(cut, "fixtureSelect"));
            Assert.Equal(new[] { "Select Market", "Match Winner" }, Options(cut, "marketSelect"));
            Assert.Equal(new[] { "Select Selection", "Away", "Home" }, Options(cut, "selectionSelect"));
        }

        [Fact]
        public void ChoosingAnotherSegment_ReloadsTheLeaguesAndFixturesOfThatSegment()
        {
            var cut = RenderWithFootballChosen();

            Choose(cut, "segmentSelect", "2");

            cut.WaitForAssertion(() => Assert.Equal(new[] { "Select League", "La Liga" }, Options(cut, "LeagueSelect")));
            Assert.Equal("200", SelectedValue(cut, "LeagueSelect"));
            Assert.Equal(new[] { "Select Fixture", "Sevilla vs Betis" }, Options(cut, "fixtureSelect"));
            _backofficeService.Verify(s => s.GetLeagueLookup(Football, 2, It.IsAny<LanguageCode>()), Times.Once);
        }

        [Fact]
        public void WithACompleteSelection_Next_OpensTheBoostDialogForIt()
        {
            var cut = RenderWithFootballChosen();

            ClickNext(cut);

            cut.WaitForAssertion(() => Assert.Single(_opened));
            var config = Assert.Single(_boostRequests);
            Assert.Equal(("10", "1", "100", "F100", "1"), (config.SportId, config.SegmentId, config.LeagueId, config.ProviderFixtureId, config.MarketTemplateId));
            var model = _opened[0].TryGet<AddPriceBoostFilterModel>("AddPriceBoostFilterModel");
            Assert.Equal(("F100", "Arsenal vs Chelsea", "Match Winner"), (model.ProviderFixtureId, model.FixtureName, model.MarketName));
        }

        [Fact]
        public void ResettingTheLeague_EmptiesTheDropdownsBelow_AndNextDoesNothing()
        {
            var cut = RenderWithFootballChosen();

            Choose(cut, "LeagueSelect", "0");

            cut.WaitForAssertion(() => Assert.Equal(new[] { "Select Fixture" }, Options(cut, "fixtureSelect")));
            Assert.Equal(new[] { "Select Market" }, Options(cut, "marketSelect"));
            Assert.Equal(new[] { "Select Selection" }, Options(cut, "selectionSelect"));

            ClickNext(cut);

            Assert.Empty(_boostRequests);
            Assert.Empty(_opened);
        }

        [Fact]
        public void ResettingTheSegment_EmptiesTheLeaguesBelow_AndNextDoesNothing()
        {
            var cut = RenderWithFootballChosen();

            Choose(cut, "segmentSelect", "0");

            cut.WaitForAssertion(() => Assert.Equal(new[] { "Select League" }, Options(cut, "LeagueSelect")));
            Assert.Equal(new[] { "Select Fixture" }, Options(cut, "fixtureSelect"));

            ClickNext(cut);

            Assert.Empty(_opened);
        }

        [Fact]
        public void ResettingTheFixture_EmptiesTheMarketsAndSelections_AndNextDoesNothing()
        {
            var cut = RenderWithFootballChosen();

            Choose(cut, "fixtureSelect", string.Empty);

            cut.WaitForAssertion(() => Assert.Equal(new[] { "Select Market" }, Options(cut, "marketSelect")));
            Assert.Equal(new[] { "Select Selection" }, Options(cut, "selectionSelect"));

            ClickNext(cut);

            Assert.Empty(_opened);
        }

        [Fact]
        public void ResettingTheMarket_EmptiesTheSelections_AndNextDoesNothing()
        {
            var cut = RenderWithFootballChosen();

            Choose(cut, "marketSelect", string.Empty);

            cut.WaitForAssertion(() => Assert.Equal(new[] { "Select Selection" }, Options(cut, "selectionSelect")));

            ClickNext(cut);

            Assert.Empty(_opened);
        }

        [Fact]
        public void ResettingTheSelection_NextDoesNothing()
        {
            var cut = RenderWithFootballChosen();

            Choose(cut, "selectionSelect", string.Empty);
            ClickNext(cut);

            Assert.Empty(_opened);
        }

        [Fact]
        public void ChoosingTheSelectSportPlaceholder_ClearsEverything_WithoutThrowing()
        {
            var cut = RenderWithFootballChosen();

            Choose(cut, "sportSelect", "0");

            cut.WaitForAssertion(() => Assert.Equal(new[] { "Select Segment" }, Options(cut, "segmentSelect")));
            Assert.Equal(new[] { "Select League" }, Options(cut, "LeagueSelect"));
            Assert.Equal(new[] { "Select Fixture" }, Options(cut, "fixtureSelect"));
            Assert.Equal(new[] { "Select Market" }, Options(cut, "marketSelect"));
            Assert.Equal(new[] { "Select Selection" }, Options(cut, "selectionSelect"));

            ClickNext(cut);

            Assert.Empty(_opened);
        }

        [Fact]
        public void ChangingToASportWithoutSegments_DoesNotKeepTheSegmentOrLeagueOfTheOldSport()
        {
            var cut = RenderWithFootballChosen();

            Choose(cut, "sportSelect", Basketball.ToString());

            cut.WaitForAssertion(() => Assert.Equal(new[] { "Select Segment" }, Options(cut, "segmentSelect")));
            Assert.Equal(new[] { "Select League" }, Options(cut, "LeagueSelect"));
            Assert.Equal(new[] { "Select Fixture" }, Options(cut, "fixtureSelect"));

            // Only the football league asked the Trading API for fixtures, a league of 0 is never sent.
            Assert.Equal(new[] { 100 }, _fixtureRequests.Select(r => r.LeagueId));

            ClickNext(cut);

            Assert.Empty(_opened);
        }

        [Fact]
        public void WhenTheLookupsFail_TheDropdownsStayEmpty_WithoutThrowing()
        {
            _backofficeService.Setup(s => s.GetSportLookup(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<SportsLookupResponse> { StatusCode = StatusCode.Error, Message = "Error (Bad Gateway)" });

            var cut = Render();

            cut.WaitForAssertion(() => Assert.Equal(new[] { "Select Sport" }, Options(cut, "sportSelect")));
            Assert.Equal(new[] { "Select Segment" }, Options(cut, "segmentSelect"));
        }

        [Fact]
        public void WhenTheSegmentLookupFails_ChoosingASportLeavesTheSegmentsAndLeaguesEmpty()
        {
            _backofficeService.Setup(s => s.GetSegmentLookup(It.IsAny<int>(), It.IsAny<LanguageCode>()))
                .ReturnsAsync(new BaseResponse<SegmentLookupResponse> { StatusCode = StatusCode.Error, Message = "Error (Bad Gateway)" });
            var cut = Render();
            cut.WaitForAssertion(() => Assert.Equal(3, Options(cut, "sportSelect").Count));

            Choose(cut, "sportSelect", Football.ToString());

            cut.WaitForAssertion(() => Assert.Equal(Football.ToString(), SelectedValue(cut, "sportSelect")));
            Assert.Equal(new[] { "Select Segment" }, Options(cut, "segmentSelect"));
            Assert.Equal(new[] { "Select League" }, Options(cut, "LeagueSelect"));
            Assert.Empty(_fixtureRequests);
        }

        // ---------------------------------------------------------------
        // Back from the Next dialog
        // ---------------------------------------------------------------

        private static readonly string[] AllDropdowns = { "sportSelect", "segmentSelect", "LeagueSelect", "fixtureSelect", "marketSelect", "selectionSelect" };

        // The Back arrow of the Next dialog opens this dialog again with the model it was given (ManageBoost.OnCancelCallback).
        private IRenderedComponent<AddBoostedOddsModal> RenderBackTo(AddPriceBoostFilterModel previous)
        {
            var parameters = new ModalParameters();
            parameters.Add("Region", "hk");
            parameters.Add("AddPriceBoostFilterModel", previous);

            return Render<AddBoostedOddsModal>(ps => ps.AddCascadingValue(parameters));
        }

        private AddPriceBoostFilterModel NextModelOf(IRenderedComponent<AddBoostedOddsModal> cut)
        {
            ClickNext(cut);
            cut.WaitForAssertion(() => Assert.Single(_opened));

            return _opened[0].TryGet<AddPriceBoostFilterModel>("AddPriceBoostFilterModel")!;
        }

        [Fact]
        public void GoingBackFromNext_KeepsTheChoicesMadeBefore()
        {
            var cut = RenderWithFootballChosen();
            Choose(cut, "segmentSelect", "2");
            cut.WaitForAssertion(() => Assert.Equal("200", SelectedValue(cut, "LeagueSelect")));
            var chosen = AllDropdowns.Select(id => SelectedValue(cut, id)).ToList();
            Assert.Equal(new[] { "10", "2", "200", "F200" }, chosen.Take(4));
            var model = NextModelOf(cut);

            var back = RenderBackTo(model);

            back.WaitForAssertion(() => Assert.Equal(new[] { "Select Sport", "Football", "Basketball" }, Options(back, "sportSelect")));
            Assert.Equal(chosen, AllDropdowns.Select(id => SelectedValue(back, id)).ToList());
            Assert.Equal(new[] { "Select League", "La Liga" }, Options(back, "LeagueSelect"));
            Assert.Equal(new[] { "Select Fixture", "Sevilla vs Betis" }, Options(back, "fixtureSelect"));
            Assert.True(back.Find("#matchType").HasAttribute("checked"));
            Assert.False(back.Find("#outrights").HasAttribute("checked"));
        }

        [Fact]
        public void GoingBackFromNext_LoadsTheFixturesAgain_SoAnotherMarketCanStillBeChosen()
        {
            var cut = RenderWithFootballChosen();
            var model = NextModelOf(cut);
            _fixtureRequests.Clear();

            var back = RenderBackTo(model);

            back.WaitForAssertion(() => Assert.Single(_fixtureRequests));
            Assert.Equal((Football, 1, 100, (int)FixtureType.Match), (_fixtureRequests[0].SportId, _fixtureRequests[0].SegmentId, _fixtureRequests[0].LeagueId, _fixtureRequests[0].FixtureTypeId));

            Choose(back, "marketSelect", "1|");

            back.WaitForAssertion(() => Assert.Equal(new[] { "Select Selection", "Away", "Home" }, Options(back, "selectionSelect")));
        }

        [Fact]
        public void GoingBackFromNext_ReturnsToTheOutrightsTab()
        {
            _tradingService
                .Setup(s => s.GetSportsBookOutrightFixtures(It.IsAny<MarketFixtureRequest>()))
                .Callback<MarketFixtureRequest>(r => _fixtureRequests.Add(r))
                .ReturnsAsync((MarketFixtureRequest r) => Reply(new MarketFixtureResponse { Fixtures = FixturesOf(r.LeagueId) }));
            var cut = Render();
            cut.WaitForAssertion(() => Assert.Equal(3, Options(cut, "sportSelect").Count));
            cut.Find("#outrights").Click();
            Choose(cut, "sportSelect", Football.ToString());
            cut.WaitForAssertion(() => Assert.Equal("F100", SelectedValue(cut, "fixtureSelect")));
            var model = NextModelOf(cut);
            Assert.Equal((int)FixtureType.Outright, model.FixtureTypeId);
            _fixtureRequests.Clear();

            var back = RenderBackTo(model);

            back.WaitForAssertion(() => Assert.Single(_fixtureRequests));
            Assert.Equal((int)FixtureType.Outright, _fixtureRequests[0].FixtureTypeId);
            Assert.True(back.Find("#outrights").HasAttribute("checked"));
            Assert.False(back.Find("#matchType").HasAttribute("checked"));
            Assert.Equal("F100", SelectedValue(back, "fixtureSelect"));
        }

        [Fact]
        public void GoingBackFromNext_WhenTheFixtureIsNoLongerListed_ClearsItAndTheDropdownsBelow()
        {
            var cut = RenderWithFootballChosen();
            var model = NextModelOf(cut);
            _tradingService
                .Setup(s => s.GetSportsBookFixtures(It.IsAny<MarketFixtureRequest>()))
                .ReturnsAsync(Reply(new MarketFixtureResponse { Fixtures = new List<MarketFixtureResponse.FixtureModel>() }));

            var back = RenderBackTo(model);

            back.WaitForAssertion(() => Assert.Equal(new[] { "Select Sport", "Football", "Basketball" }, Options(back, "sportSelect")));
            Assert.Equal(("10", "1", "100"), (SelectedValue(back, "sportSelect"), SelectedValue(back, "segmentSelect"), SelectedValue(back, "LeagueSelect")));
            Assert.Equal(new[] { "Select Fixture" }, Options(back, "fixtureSelect"));
            Assert.Equal(new[] { "Select Market" }, Options(back, "marketSelect"));
            Assert.Equal(new[] { "Select Selection" }, Options(back, "selectionSelect"));
        }
    }
}
