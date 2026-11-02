using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.OddsEngine.Configuration;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Backoffice.Web.Tests.OddsEngine
{
    // BO-142: the template editor (and the My Templates Logs dialog) took their market groups and market list from the old
    // Odds Engine Trading API, which does not resolve on DEV or STG, so the editor showed "No Markets Available" and the Logs
    // filter threw. They now come from the BO API's Sportsbook data (GetConfigMarketTemplateGroups and GetConfigMarketLookup),
    // and a failed call is shown as an error instead of looking like an empty list.
    public class ConfigMarketListLoadingTests : BunitTestContext
    {
        private const string MarketsAlert = "The markets could not be loaded";
        private const long TemplateId = 5;

        private readonly Mock<IBackofficeService> _backofficeService = new();
        private readonly Mock<ITradingService> _tradingService = new();

        public ConfigMarketListLoadingTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;

            // Every component under Backoffice.Web picks up the @inject list in _Imports.razor, so bUnit needs them all.
            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());

            var authenticationService = new Mock<IAuthenticationService>();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            var translationState = new TranslationState(translationService.Object);
            var userState = new UserState();
            userState.SetTradingClients(new List<PlatformClientResponse>());

            Services.AddSingleton<IConfiguration>(configuration);
            Services.AddSingleton<IModalService>(new ModalService(translationState));
            Services.AddSingleton(new Mock<IPreRenderService>().Object);
            Services.AddSingleton(_tradingService.Object);
            Services.AddSingleton(new Mock<IProviderMappingService>().Object);
            Services.AddSingleton(_backofficeService.Object);
            Services.AddSingleton(authenticationService.Object);
            Services.AddSingleton(new Mock<ITableauService>().Object);
            Services.AddSingleton(new Mock<ICommonService>().Object);
            Services.AddSingleton(translationState);
            Services.AddSingleton(userState);
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                authenticationService.Object,
                sp.GetRequiredService<NavigationManager>()));

            var authContext = this.AddAuthorization();
            authContext.SetAuthorized("test-user");
            authContext.SetPolicies("sbc-config", "sbc-config-canedit");

            _backofficeService
                .Setup(s => s.GetSportLookup(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<SportsLookupResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new SportsLookupResponse { SportLookup = new List<SportLookupModel> { new() { SportId = 4, SportName = "Basketball" }, new() { SportId = 17, SportName = "American Football" } } }
                });
        }

        private static BaseResponse<T> Ok<T>(T result) => new() { StatusCode = StatusCode.Success, Message = "Success", Result = result };

        private static BaseResponse<T> Failed<T>(string message = "Error: Name or service not known") => new() { StatusCode = StatusCode.Error, Message = message };

        private static MarketLookupResponse Markets(params (string Id, string Name)[] markets) => new()
        {
            MarketLookup = markets.Select(m => new MarketLookupModel { MarketTemplateId = m.Id, MarketTemplateName = m.Name }).ToList()
        };

        private static MarketTemplateGroupResponse GroupsOfBasketball() => new()
        {
            Sports = new List<MarketTemplateGroupResponse.SportMarketTemplateGroup>
            {
                new()
                {
                    SportId = 4,
                    Prematch = new List<MarketTemplateGroup>
                    {
                        new() { MarketTemplateGroupId = 1, MarketTemplateGroupName = "Main Market", SortOrder = 1, MarketTemplateIds = new List<string> { "115", "147" } },
                        new() { MarketTemplateGroupId = 3, MarketTemplateGroupName = "Team Props", SortOrder = 3, MarketTemplateIds = new List<string> { "8471" } }
                    }
                }
            }
        };

        private void ServeTemplate(int sportId) =>
            _backofficeService
                .Setup(s => s.GetConfigTemplateById(TemplateId))
                .ReturnsAsync(Ok(new ConfigTemplateModel { Id = TemplateId, Name = "Dev Template Test-PreMatch", SportId = sportId, FixtureStatusId = (int)Scope.Prematch }));

        private void ServeConfigMarkets(BaseResponse<MarketTemplateGroupResponse> groups, BaseResponse<MarketLookupResponse> lookup)
        {
            _backofficeService
                .Setup(s => s.GetConfigMarketTemplateGroups(It.IsAny<CancellationToken>()))
                .ReturnsAsync(groups);
            _backofficeService
                .Setup(s => s.GetConfigMarketLookup(It.IsAny<MarketLookupRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(lookup);
        }

        private IRenderedComponent<EditConfigTemplateFilter> RenderEditFilter(Action<EditConfigTemplateFilterModel> onChange) =>
            Render<EditConfigTemplateFilter>(ps => ps
                .Add(p => p.ConfigTemplateId, TemplateId)
                .Add(p => p.EditConfigTemplateFilterCallback, EventCallback.Factory.Create(this, onChange)));

        // ---- The template editor ------------------------------------------------------------------------------

        [Fact]
        public void ASportWithGroups_ListsThemFromTheConfigCalls()
        {
            ServeTemplate(4);
            ServeConfigMarkets(Ok(GroupsOfBasketball()), Ok(Markets(("115", "Moneyline"), ("147", "Spread"), ("8471", "Team Total"))));
            EditConfigTemplateFilterModel? loaded = null;

            var cut = RenderEditFilter(m => loaded = m);

            cut.WaitForAssertion(() => Assert.NotNull(loaded));
            Assert.Equal(new[] { "Main Market", "Team Props" }, loaded!.MarketTemplateGroups.Select(g => g.MarketTemplateGroup.MarketTemplateGroupName).ToArray());
            Assert.DoesNotContain("No Markets Available", cut.Markup);
            Assert.DoesNotContain(MarketsAlert, cut.Markup);
        }

        [Fact]
        public void ASportWithoutGroups_GetsOneAllMarketsGroupOfEveryMarket()
        {
            ServeTemplate(17);
            ServeConfigMarkets(Ok(GroupsOfBasketball()), Ok(Markets(("9484", "4th Quarter Result"), ("9485", "Totals"), ("9486", "Spread"))));
            EditConfigTemplateFilterModel? loaded = null;

            var cut = RenderEditFilter(m => loaded = m);

            cut.WaitForAssertion(() => Assert.NotNull(loaded));
            var group = Assert.Single(loaded!.MarketTemplateGroups).MarketTemplateGroup;
            Assert.Equal("All Markets", group.MarketTemplateGroupName);
            Assert.Equal(new[] { "9484", "9485", "9486" }, group.MarketTemplateIds.ToArray());
            Assert.DoesNotContain("No Markets Available", cut.Markup);
        }

        [Fact]
        public void TheMarketsAreNotAskedOfTheOddsEngineApi()
        {
            ServeTemplate(17);
            ServeConfigMarkets(Ok(new MarketTemplateGroupResponse()), Ok(Markets(("9484", "4th Quarter Result"))));
            EditConfigTemplateFilterModel? loaded = null;

            var cut = RenderEditFilter(m => loaded = m);

            cut.WaitForAssertion(() => Assert.NotNull(loaded));
            _tradingService.Verify(t => t.GetMarketTemplateGroups(It.IsAny<int>()), Times.Never);
            _tradingService.Verify(t => t.GetMarketLookup(It.IsAny<MarketLookupRequest>()), Times.Never);
            _backofficeService.Verify(s => s.GetConfigMarketLookup(It.Is<MarketLookupRequest>(r => r.SportId == 17 && r.FixtureTypeId == (int)FixtureType.Match), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void AFailedGroupsCall_ShowsAnErrorMessage_NotJustNoMarkets()
        {
            ServeTemplate(17);
            ServeConfigMarkets(Failed<MarketTemplateGroupResponse>("Error (Name or service not known (api.oddsengine.svc:3000))"), Ok(Markets(("9484", "4th Quarter Result"))));
            EditConfigTemplateFilterModel? loaded = null;

            var cut = RenderEditFilter(m => loaded = m);

            cut.WaitForAssertion(() => Assert.Contains(MarketsAlert, cut.Markup));
            Assert.Contains("alert-danger", cut.Markup);
            Assert.DoesNotContain("oddsengine", cut.Markup); // the API's text goes to the log, not the screen
            Assert.Contains("No Markets Available", cut.Markup);
        }

        [Fact]
        public void AFailedMarketListCall_ShowsAnErrorMessage()
        {
            ServeTemplate(17);
            ServeConfigMarkets(Ok(new MarketTemplateGroupResponse()), Failed<MarketLookupResponse>("Error: MongoDB timed out"));
            EditConfigTemplateFilterModel? loaded = null;

            var cut = RenderEditFilter(m => loaded = m);

            cut.WaitForAssertion(() => Assert.Contains(MarketsAlert, cut.Markup));
            Assert.DoesNotContain("MongoDB", cut.Markup);
            Assert.Empty(loaded?.MarketTemplateGroups ?? new List<MarketGroupNavigationModel>());
        }

        [Fact]
        public void ASuccessfulLoad_ShowsNoErrorMessage()
        {
            ServeTemplate(17);
            ServeConfigMarkets(Ok(new MarketTemplateGroupResponse()), Ok(Markets(("9484", "4th Quarter Result"))));
            EditConfigTemplateFilterModel? loaded = null;

            var cut = RenderEditFilter(m => loaded = m);

            cut.WaitForAssertion(() => Assert.NotNull(loaded));
            Assert.DoesNotContain(MarketsAlert, cut.Markup);
        }

        // ---- The My Templates Logs filter ---------------------------------------------------------------------

        private async Task<IRenderedComponent<ViewLogsFilter>> ChooseSport(int sportId)
        {
            _backofficeService
                .Setup(s => s.GetSegmentLookup(It.IsAny<int>(), It.IsAny<LanguageCode>()))
                .ReturnsAsync(Ok(new SegmentLookupResponse()));
            _backofficeService
                .Setup(s => s.GetLeagueLookup(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<LanguageCode>()))
                .ReturnsAsync(Ok(new LeagueLookupResponse()));

            var cut = Render<ViewLogsFilter>();
            cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.ConfigLogsFilterModel?.Sports));

            await cut.InvokeAsync(() => cut.Instance.OnSportsChangedEvent(new ChangeEventArgs { Value = sportId.ToString() }));

            return cut;
        }

        [Fact]
        public async Task TheLogsFilter_ListsTheGroupsAndMarketsFromTheConfigCalls()
        {
            ServeConfigMarkets(Ok(GroupsOfBasketball()), Ok(Markets(("115", "Moneyline"), ("147", "Spread"), ("8471", "Team Total"))));

            var cut = await ChooseSport(4);

            Assert.Equal(new[] { "Main Market", "Team Props" }, cut.Instance.ConfigLogsFilterModel.MarketTemplateGroups.Select(g => g.Name).ToArray());
            Assert.Equal(new[] { "115", "147", "8471" }, cut.Instance.ConfigLogsFilterModel.Markets.Select(m => m.Value).ToArray());
            _tradingService.Verify(t => t.GetMarketTemplateGroups(It.IsAny<int>()), Times.Never);
            _tradingService.Verify(t => t.GetMarketLookup(It.IsAny<MarketLookupRequest>()), Times.Never);
        }

        [Fact]
        public async Task TheLogsFilter_WhenTheMarketCallsFail_ChoosingASportDoesNotThrow_AndTheListsAreEmpty()
        {
            ServeConfigMarkets(Failed<MarketTemplateGroupResponse>(), Failed<MarketLookupResponse>());

            var cut = await ChooseSport(4);

            Assert.Empty(cut.Instance.ConfigLogsFilterModel.MarketTemplateGroups);
            Assert.Empty(cut.Instance.ConfigLogsFilterModel.Markets);
        }
    }
}
