using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.OddsEngine.MonitoringDetails;
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
    // BO-154: the Monitoring market group filter and the Close Markets dialog read their market groups from the old Odds Engine
    // Trading API, which does not answer on DEV and STG, and showed nothing when the call failed. They now read the same
    // Sportsbook-backed groups as the Config pages (GetConfigMarketTemplateGroups), show an error when the load fails, and show one
    // "All Markets" group for a sport that has no group rows (the filter also no longer calls back with null).
    public class MonitoringMarketGroupsTests : BunitTestContext
    {
        private const int SportWithGroups = 4;
        private const int SportWithoutGroups = 17;
        private const string LoadError = "The market groups could not be loaded";

        private readonly Mock<IBackofficeService> _backofficeService = new();

        public MonitoringMarketGroupsTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;

            // Every component under Backoffice.Web picks up the @inject list in _Imports.razor, so bUnit needs them all.
            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            Services.AddSingleton<IConfiguration>(configuration);
            Services.AddSingleton(new Mock<IModalService>().Object);
            Services.AddSingleton(new Mock<IPreRenderService>().Object);
            Services.AddSingleton(new Mock<ITradingService>().Object);
            Services.AddSingleton(new Mock<IProviderMappingService>().Object);
            Services.AddSingleton(_backofficeService.Object);
            Services.AddSingleton(new Mock<IAuthenticationService>().Object);
            Services.AddSingleton(new Mock<ITableauService>().Object);
            Services.AddSingleton(new Mock<ICommonService>().Object);
            Services.AddSingleton(new TranslationState(translationService.Object));
            Services.AddSingleton<UserState>();
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                new Mock<IAuthenticationService>().Object,
                sp.GetRequiredService<NavigationManager>()));
        }

        private static MarketFixtureResponse.FixtureModel Fixture(int sportId, params string[] marketTemplateIds) => new()
        {
            SportId = sportId,
            HomeName = "Home",
            AwayName = "Away",
            Markets = marketTemplateIds
                .Select(id => new MarketFixtureResponse.MarketModel { MarketTemplateId = id, MarketName = $"Market {id}", MarketStatusId = 1 })
                .ToList()
        };

        private static MarketTemplateGroup Group(string name, int sortOrder, params string[] marketTemplateIds) => new()
        {
            MarketTemplateGroupName = name,
            SortOrder = sortOrder,
            MarketTemplateIds = marketTemplateIds.ToList()
        };

        // Sport 4 has a Live and a Prematch group, and another sport has a group of its own that must not show.
        private void SetUpGroups()
        {
            var sport = new MarketTemplateGroupResponse.SportMarketTemplateGroup { SportId = SportWithGroups };
            sport.Live.Add(Group("Main", 1, "9484"));
            sport.Prematch.Add(Group("Totals", 2, "9485"));

            var other = new MarketTemplateGroupResponse.SportMarketTemplateGroup { SportId = 1 };
            other.Live.Add(Group("Soccer Only", 1, "1"));

            _backofficeService
                .Setup(s => s.GetConfigMarketTemplateGroups(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BaseResponse<MarketTemplateGroupResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = "Success",
                    Result = new MarketTemplateGroupResponse { Sports = new List<MarketTemplateGroupResponse.SportMarketTemplateGroup> { sport, other } }
                });
        }

        private void SetUpFailedGroups() =>
            _backofficeService
                .Setup(s => s.GetConfigMarketTemplateGroups(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BaseResponse<MarketTemplateGroupResponse> { StatusCode = StatusCode.Error, Message = "Error (Bad Request)" });

        private void VerifyTheOldRouteIsNotCalled() =>
            _backofficeService.Verify(s => s.GetMarketTemplateGroups(It.IsAny<int>()), Times.Never);

        // ---- The market group filter on the Monitoring detail page --------------------------------------------

        private IRenderedComponent<MarketGroupFilter> RenderFilter(MarketFixtureResponse.FixtureModel fixture, List<MarketGroupNavigationModel> callbacks) =>
            Render<MarketGroupFilter>(ps => ps
                .Add(p => p.SportId, fixture.SportId)
                .Add(p => p.Fixture, fixture)
                .Add(p => p.OnInputSearchCallback, EventCallback.Factory.Create<string>(this, _ => { }))
                .Add(p => p.OnMarketTemplateGroupCallback, EventCallback.Factory.Create<MarketGroupNavigationModel>(this, group => callbacks.Add(group))));

        private static List<string> FilterButtons(IRenderedComponent<MarketGroupFilter> cut) =>
            cut.FindAll("a.btn-market-group").Select(a => a.TextContent.Trim()).ToList();

        [Fact]
        public void TheFilter_ShowsTheGroupsOfTheFixturesSport_FromTheConfigRoute_AndSelectsTheFirst()
        {
            SetUpGroups();
            var callbacks = new List<MarketGroupNavigationModel>();

            var cut = RenderFilter(Fixture(SportWithGroups, "9484", "9485"), callbacks);

            cut.WaitForAssertion(() => Assert.Equal(new[] { "Main", "Totals" }, FilterButtons(cut)));
            var selected = Assert.Single(callbacks);
            Assert.Equal("Main", selected.MarketTemplateGroup.MarketTemplateGroupName);
            Assert.DoesNotContain(LoadError, cut.Markup);
            VerifyTheOldRouteIsNotCalled();
        }

        [Fact]
        public void TheFilter_ForASportWithNoGroups_ShowsOneAllMarketsGroup_HoldingEveryMarketOfTheFixture()
        {
            SetUpGroups();
            var callbacks = new List<MarketGroupNavigationModel>();

            var cut = RenderFilter(Fixture(SportWithoutGroups, "100", "200"), callbacks);

            cut.WaitForAssertion(() => Assert.Equal(new[] { "All Markets" }, FilterButtons(cut)));
            var selected = Assert.Single(callbacks);
            Assert.Equal("All Markets", selected.MarketTemplateGroup.MarketTemplateGroupName);
            Assert.Equal(new[] { "100", "200" }, selected.MarketTemplateGroup.MarketTemplateIds.OrderBy(id => id).ToArray());
        }

        [Fact]
        public void TheFilter_WhenTheGroupsFailToLoad_ShowsAnError_AndDoesNotCallBack()
        {
            SetUpFailedGroups();
            var callbacks = new List<MarketGroupNavigationModel>();

            var cut = RenderFilter(Fixture(SportWithGroups, "9484"), callbacks);

            cut.WaitForAssertion(() => Assert.Contains(LoadError, cut.Markup));
            Assert.Empty(FilterButtons(cut));
            Assert.Empty(callbacks);
            VerifyTheOldRouteIsNotCalled();
        }

        [Fact]
        public void TheFilter_WhenNoGroupMatchesTheFixturesMarkets_DoesNotCallBackWithNull()
        {
            SetUpGroups();
            var callbacks = new List<MarketGroupNavigationModel>();

            // the fixture only has a market that no group of sport 4 lists
            var cut = RenderFilter(Fixture(SportWithGroups, "9999"), callbacks);

            cut.WaitForAssertion(() => Assert.Empty(FilterButtons(cut)));
            _backofficeService.Verify(s => s.GetConfigMarketTemplateGroups(It.IsAny<CancellationToken>()), Times.Once);
            Assert.Empty(callbacks);
            Assert.DoesNotContain(LoadError, cut.Markup);
        }

        // ---- The Close Markets dialog -------------------------------------------------------------------------

        private IRenderedComponent<CloseMarketsModal> RenderModal(MarketFixtureResponse.FixtureModel fixture)
        {
            var parameters = new ModalParameters();
            parameters.Add("Fixture", fixture);

            return Render<CloseMarketsModal>(ps => ps.AddCascadingValue(parameters));
        }

        private static List<string> ModalGroups(IRenderedComponent<CloseMarketsModal> cut) =>
            cut.FindAll("label.btn.fw-bold").Select(l => l.TextContent.Trim()).ToList();

        [Fact]
        public void TheDialog_ShowsTheGroupsOfTheFixturesSport_FromTheConfigRoute()
        {
            SetUpGroups();

            var cut = RenderModal(Fixture(SportWithGroups, "9484", "9485"));

            cut.WaitForAssertion(() => Assert.Equal(new[] { "Main", "Totals" }, ModalGroups(cut)));
            Assert.DoesNotContain(LoadError, cut.Markup);
            VerifyTheOldRouteIsNotCalled();
        }

        [Fact]
        public void TheDialog_ForASportWithNoGroups_ShowsOneAllMarketsGroup_ThatListsEveryMarket()
        {
            SetUpGroups();

            var cut = RenderModal(Fixture(SportWithoutGroups, "100", "200"));

            cut.WaitForAssertion(() => Assert.Equal(new[] { "All Markets" }, ModalGroups(cut)));

            cut.Find("input#radio1").Click();

            cut.WaitForAssertion(() =>
            {
                Assert.Contains("Market 100", cut.Markup);
                Assert.Contains("Market 200", cut.Markup);
            });
        }

        [Fact]
        public void TheDialog_WhenTheGroupsFailToLoad_ShowsAnError_AndNoGroups()
        {
            SetUpFailedGroups();

            var cut = RenderModal(Fixture(SportWithGroups, "9484"));

            cut.WaitForAssertion(() => Assert.Contains(LoadError, cut.Markup));
            Assert.Empty(ModalGroups(cut));
            VerifyTheOldRouteIsNotCalled();
        }
    }
}
