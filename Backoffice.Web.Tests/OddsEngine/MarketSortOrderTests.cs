using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;
using MarketSortOrderFilter = Backoffice.Web.Components.OddsEngine.MarketSortOrder.Filter;
using MarketSortOrderPage = Backoffice.Web.Pages.OddsEngine.MarketSortOrder;

namespace Backoffice.Web.Tests.OddsEngine
{
    // Regression tests for BO-23: switching a Market Sort Order filter must hide the
    // table (preloader) for the whole reload instead of briefly re-rendering the old
    // sport's rows, and the table must come back with the new data once loaded.
    public class MarketSortOrderTests : TestContext
    {
        private static readonly Guid ClientId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private readonly Mock<IBackofficeService> _backofficeService = new();

        public MarketSortOrderTests()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());
            var translationState = new TranslationState(translationService.Object);

            var authenticationService = new Mock<IAuthenticationService>();
            authenticationService
                .Setup(s => s.AuthenticateToken(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<TokenModel> { StatusCode = StatusCode.Success });

            var userState = new UserState();
            userState.Model.Id = "test-user";
            userState.Model.TradingClients.Add(new PlatformClientResponse
            {
                ClientId = ClientId,
                ClientCode = "GSA",
                ClientName = "GameSnap LiboAsia"
            });
            userState.Model.Claims.Add(new Claim("SBC.OE.MarketSetting.SortOrder", "1"));

            Services.AddSingleton<IConfiguration>(configuration);
            Services.AddSingleton<IModalService>(new ModalService(translationState));
            Services.AddSingleton(new Mock<IPreRenderService>().Object);
            Services.AddSingleton(new Mock<ITradingService>().Object);
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

            var authContext = this.AddTestAuthorization();
            authContext.SetAuthorized("test-user");
            authContext.SetPolicies("sbc-market-sortorder", "sbc-market-sortorder-canedit");

            JSInterop.Mode = JSRuntimeMode.Loose;

            _backofficeService
                .Setup(s => s.GetSportLookup(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<SportsLookupResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new SportsLookupResponse
                    {
                        SportLookup = new List<SportLookupModel>
                        {
                            new SportLookupModel { SportId = 1, SportName = "Football" },
                            new SportLookupModel { SportId = 2, SportName = "Basketball" }
                        }
                    }
                });

            _backofficeService
                .Setup(s => s.GetClientMarketTemplateGroups(ClientId, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new BaseResponse<ClientMarketTemplateGroupResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ClientMarketTemplateGroupResponse
                    {
                        ClientId = ClientId,
                        ClientMarketTemplateGroups = new List<ClientMarketTemplateGroupDTO>
                        {
                            new ClientMarketTemplateGroupDTO { MarketTemplateGroupId = 1, Name = "All Markets", MarketTemplateIds = "m1,m2", SortOrder = 1 },
                            new ClientMarketTemplateGroupDTO { MarketTemplateGroupId = 2, Name = "Main Market", MarketTemplateIds = "m1", SortOrder = 2 }
                        }
                    }
                });

            SetupMarketTemplates(1, SportOneTemplates());
        }

        private void SetupMarketTemplates(int sportId, List<ClientMarketTemplateModel> templates) =>
            _backofficeService
                .Setup(s => s.GetClientMarketTemplates("GSA", sportId, It.IsAny<int>(), It.IsAny<LanguageCode>()))
                .ReturnsAsync(TemplatesResponse(sportId, templates));

        private static BaseResponse<ClientMarketTemplateResponse> TemplatesResponse(int sportId, List<ClientMarketTemplateModel> templates) =>
            new()
            {
                StatusCode = StatusCode.Success,
                Result = new ClientMarketTemplateResponse
                {
                    ClientId = ClientId,
                    SportId = sportId,
                    MarketTemplates = templates
                }
            };

        private static List<ClientMarketTemplateModel> SportOneTemplates() =>
            new()
            {
                new ClientMarketTemplateModel { MarketTemplateId = "m1", MarketTemplateName = "Match Result", MarketTemplateGroupNames = new List<string> { "Main Market" }, SortOrder = 1 },
                new ClientMarketTemplateModel { MarketTemplateId = "m2", MarketTemplateName = "Total Goals", SortOrder = 2 }
            };

        private static List<ClientMarketTemplateModel> SportTwoTemplates() =>
            new()
            {
                new ClientMarketTemplateModel { MarketTemplateId = "b1", MarketTemplateName = "Point Spread", MarketTemplateGroupNames = new List<string> { "Main Market" }, SortOrder = 1 }
            };

        [Fact]
        public void SportChangeRaisesPreloaderBeforeReloadingData()
        {
            var events = new List<string>();

            var cut = RenderComponent<MarketSortOrderFilter>(ps => ps
                .Add(p => p.Region, "HK")
                .Add(p => p.IsPreLoader, false)
                .Add(p => p.IsPreLoaderChanged, (bool value) => events.Add($"preloader:{value}"))
                .Add(p => p.OnInitializeEventCallback, (MarketSortOrderFilterModel _) => events.Add("init"))
                .Add(p => p.OnCancelCallback, () => events.Add("cancel")));

            // Initial load fires its own init/preloader events; only the change matters here.
            cut.WaitForAssertion(() => Assert.Contains("init", events));
            events.Clear();

            cut.FindAll("select")[1].Change("2");

            // The page must be told to show its preloader before the reload starts,
            // otherwise it re-renders the previous sport's rows first (BO-23 flicker).
            cut.WaitForAssertion(() =>
            {
                Assert.Contains("preloader:True", events);
                Assert.Contains("init", events);
                Assert.True(events.IndexOf("preloader:True") < events.IndexOf("init"),
                    $"preloader:True must precede init, got: {string.Join(", ", events)}");
            });
        }

        [Fact]
        public void SportChangeHidesTableWhileLoadingThenRendersNewData()
        {
            var cut = RenderComponent<MarketSortOrderPage>();

            cut.WaitForAssertion(() =>
            {
                Assert.Contains("Match Result", cut.Markup);
                Assert.Contains("Total Goals", cut.Markup);
            });

            var initSelect2CountAfterLoad = JSInterop.Invocations.Count(i => i.Identifier == "initSelect2");
            Assert.True(initSelect2CountAfterLoad > 0, "select2 must be initialized after the initial load");

            // Hold the new sport's response so the loading state is observable.
            var pendingTemplates = new TaskCompletionSource<BaseResponse<ClientMarketTemplateResponse>>();
            _backofficeService
                .Setup(s => s.GetClientMarketTemplates("GSA", 2, It.IsAny<int>(), It.IsAny<LanguageCode>()))
                .Returns(pendingTemplates.Task);

            cut.FindAll(".content-filter select")[1].Change("2");

            // While the reload is pending, neither sport's rows may be visible, only the preloader.
            cut.WaitForAssertion(() =>
            {
                Assert.Contains("spinner-border", cut.Markup);
                Assert.DoesNotContain("Match Result", cut.Markup);
            });

            pendingTemplates.SetResult(TemplatesResponse(2, SportTwoTemplates()));

            cut.WaitForAssertion(() =>
            {
                Assert.Contains("Point Spread", cut.Markup);
                Assert.DoesNotContain("Match Result", cut.Markup);
                Assert.DoesNotContain("spinner-border", cut.Markup);
            });

            // The preloader flag must be released AND select2 re-initialized on the new rows.
            Assert.True(
                JSInterop.Invocations.Count(i => i.Identifier == "initSelect2") > initSelect2CountAfterLoad,
                "select2 must be re-initialized after the reload");
        }

        [Fact]
        public void OlderSlowFilterResponseDoesNotClobberNewerFasterOne()
        {
            var cut = RenderComponent<MarketSortOrderPage>();

            cut.WaitForAssertion(() => Assert.Contains("Match Result", cut.Markup));

            // Switch to Sport 2 but hold its response so it is still in flight when the
            // user switches straight back to Sport 1.
            var pendingSportTwo = new TaskCompletionSource<BaseResponse<ClientMarketTemplateResponse>>();
            _backofficeService
                .Setup(s => s.GetClientMarketTemplates("GSA", 2, It.IsAny<int>(), It.IsAny<LanguageCode>()))
                .Returns(pendingSportTwo.Task);

            cut.FindAll(".content-filter select")[1].Change("2");

            cut.WaitForAssertion(() => Assert.Contains("spinner-border", cut.Markup));

            // Switch back to Sport 1 before Sport 2's response arrives; Sport 1's mock
            // resolves immediately, so this newer request completes first.
            cut.FindAll(".content-filter select")[1].Change("1");

            cut.WaitForAssertion(() =>
            {
                Assert.Contains("Match Result", cut.Markup);
                Assert.DoesNotContain("spinner-border", cut.Markup);
            });

            // Now let the stale Sport 2 response resolve. It must not overwrite the
            // already-current Sport 1 data (BO-23 out-of-order response race).
            pendingSportTwo.SetResult(TemplatesResponse(2, SportTwoTemplates()));

            cut.WaitForAssertion(() =>
            {
                Assert.Contains("Match Result", cut.Markup);
                Assert.DoesNotContain("Point Spread", cut.Markup);
                Assert.DoesNotContain("spinner-border", cut.Markup);
            });
        }
    }
}
