using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Pages.OddsEngine;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text.Json;
using System.Web;
using Xunit;

namespace Backoffice.Web.Tests.OddsEngine
{
    // BO-142: the margins of a template are not editable until margin per client is enabled, the odds come from the provider by
    // default. The template editor shows the markets and their key and time nodes, but only the nodes (which open the Edit Config
    // popover) are view-only. The market Active toggle, ACTIVATE ALL, the template name and Save still work, and a market saves
    // with its default margins (the "101 by default is not allowed" check on Save is skipped while the margins are locked).
    public class ConfigEditTemplateReadOnlyTests : BunitTestContext
    {
        private const long TemplateId = 5;

        private readonly Mock<IBackofficeService> _backofficeService = new();
        private readonly BunitAuthorizationContext _authContext;

        public ConfigEditTemplateReadOnlyTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;

            // Every component under Backoffice.Web picks up the @inject list in _Imports.razor, so bUnit needs them all.
            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());

            var authenticationService = new Mock<IAuthenticationService>();
            authenticationService
                .Setup(s => s.AuthenticateToken(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<TokenModel> { StatusCode = StatusCode.Success });

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            var translationState = new TranslationState(translationService.Object);

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
            Services.AddSingleton(new UserState());
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                authenticationService.Object,
                sp.GetRequiredService<NavigationManager>()));

            // a user who may edit: the toggles must be locked by the page, not just hidden by the permission
            _authContext = this.AddAuthorization();
            _authContext.SetAuthorized("test-user");
            _authContext.SetPolicies("sbc-config", "sbc-config-canedit");

            _backofficeService
                .Setup(s => s.GetSportLookup(It.IsAny<string>()))
                .ReturnsAsync(Ok(new SportsLookupResponse { SportLookup = new List<SportLookupModel> { new() { SportId = 17, SportName = "American Football" } } }));
            _backofficeService
                .Setup(s => s.GetConfigTemplateById(TemplateId))
                .ReturnsAsync(Ok(new ConfigTemplateModel { Id = TemplateId, Name = "Dev Template Test-PreMatch", SportId = 17, FixtureStatusId = (int)Scope.Prematch }));
            _backofficeService
                .Setup(s => s.GetConfigTemplateMargin(TemplateId))
                .ReturnsAsync(Ok(new ConfigTemplateMarginModel()));
            _backofficeService
                .Setup(s => s.GetConfigMarketTemplateGroups(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Ok(new MarketTemplateGroupResponse()));
            _backofficeService
                .Setup(s => s.GetConfigMarketLookup(It.IsAny<MarketLookupRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Ok(new MarketLookupResponse
                {
                    MarketLookup = new List<MarketLookupModel>
                    {
                        new() { MarketTemplateId = "9484", MarketTemplateName = "4th Quarter Result" },
                        new() { MarketTemplateId = "9485", MarketTemplateName = "Totals" }
                    }
                }));
        }

        private static BaseResponse<T> Ok<T>(T result) => new() { StatusCode = StatusCode.Success, Message = "Success", Result = result };

        private IRenderedComponent<ConfigEditTemplate> RenderEditor(bool view = false)
        {
            var cut = Render<ConfigEditTemplate>(ps => ps
                .Add(p => p.Action, "edit")
                .Add(p => p.ConfigTemplateId, TemplateId)
                .Add(p => p.View, view));

            // the markets load after the page and the filter have initialised
            cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("div.accordion").Count), TimeSpan.FromSeconds(5));

            return cut;
        }

        [Fact]
        public void EveryMarketToggle_IsEnabled_ForAUserWhoMayEdit()
        {
            var cut = RenderEditor();

            var toggles = cut.FindAll("div.market-toggle input.form-check-input");

            Assert.Equal(2, toggles.Count);
            Assert.All(toggles, toggle => Assert.False(toggle.HasAttribute("disabled")));
        }

        [Fact]
        public void ActivateAll_IsEnabled()
        {
            var cut = RenderEditor();

            Assert.False(cut.Find("button.btn-activate-all").HasAttribute("disabled"));
        }

        [Fact]
        public void InViewMode_TheToggles_AndActivateAll_AreStillDisabled()
        {
            var cut = RenderEditor(view: true);

            Assert.All(cut.FindAll("div.market-toggle input.form-check-input"), toggle => Assert.True(toggle.HasAttribute("disabled")));
            Assert.True(cut.Find("button.btn-activate-all").HasAttribute("disabled"));
        }

        // BO-156: the "Market Active" / "Market Inactive" text next to a switch showed the saved state, so while a user was
        // editing it disagreed with the switch until the template was saved and reopened.

        // The template as saved: these markets are Active, every other market of the template is Inactive.
        private void ServeSavedActiveMarkets(params string[] activeMarketTemplateIds) =>
            _backofficeService
                .Setup(s => s.GetConfigTemplateMargin(TemplateId))
                .ReturnsAsync(Ok(new ConfigTemplateMarginModel
                {
                    ConfigTemplateMargins = activeMarketTemplateIds
                        .Select(id => new ConfigTemplateMargin
                        {
                            ConfigTemplateId = TemplateId,
                            SportId = 17,
                            MarketTemplateId = id,
                            FixtureTypeId = (int)FixtureType.Match,
                            FixtureStatusId = (int)Scope.Prematch,
                            IsActive = true,
                            Config = JsonSerializer.Serialize(new List<MarketMargin> { new() { T = 0, M = 101 } })
                        })
                        .ToList()
                }));

        // the text of each market, in the order the markets are listed (4th Quarter Result, then Totals)
        private static List<string> MarketTexts(IRenderedComponent<ConfigEditTemplate> cut) =>
            cut.FindAll("div.market-toggle span.market-is-active-text").Select(span => span.TextContent.Trim()).ToList();

        [Fact]
        public void TheMarketText_FollowsTheSwitch_WhileEditing()
        {
            ServeSavedActiveMarkets("9484");
            var cut = RenderEditor();
            Assert.Equal(new[] { "Market Active", "Market Inactive" }, MarketTexts(cut));

            cut.FindAll("div.market-toggle input.form-check-input")[0].Input(false);
            cut.FindAll("div.market-toggle input.form-check-input")[1].Input(true);

            cut.WaitForAssertion(() => Assert.Equal(new[] { "Market Inactive", "Market Active" }, MarketTexts(cut)));

            // and back: switching a market back to its saved state shows the saved text again
            cut.FindAll("div.market-toggle input.form-check-input")[0].Input(true);

            cut.WaitForAssertion(() => Assert.Equal(new[] { "Market Active", "Market Active" }, MarketTexts(cut)));
        }

        [Fact]
        public void TheMarketText_IsTheSavedState_ForAUserWhoCannotEdit()
        {
            ServeSavedActiveMarkets("9484");
            _authContext.SetPolicies("sbc-config"); // no sbc-config-canedit: the switches are shown but locked
            var cut = RenderEditor();

            Assert.Equal(new[] { "Market Active", "Market Inactive" }, MarketTexts(cut));
            Assert.All(cut.FindAll("div.market-toggle input.form-check-input"), toggle => Assert.True(toggle.HasAttribute("disabled")));
        }

        [Fact]
        public void TheMarketText_IsTheSavedState_InViewMode()
        {
            ServeSavedActiveMarkets("9485");
            var cut = RenderEditor(view: true);

            Assert.Equal(new[] { "Market Inactive", "Market Active" }, MarketTexts(cut));
        }

        [Fact]
        public void TurningAMarketActive_AndSaving_SavesItWithTheDefaultMargins_WithoutThe101Alert()
        {
            MarginRequest? saved = null;
            _backofficeService
                .Setup(s => s.AddOrUpdateConfigTemplateMargin(It.IsAny<ConfigTemplateMarginModel>()))
                .Callback<ConfigTemplateMarginModel>(request => saved = new MarginRequest(request.ConfigTemplateMargins))
                .ReturnsAsync(Ok(true));
            _backofficeService
                .Setup(s => s.AddActivityLog(It.IsAny<ActivityLogModel>()))
                .ReturnsAsync(Ok(new MessageResponse()));
            var cut = RenderEditor();

            cut.FindAll("div.market-toggle input.form-check-input")[0].Input(true);

            cut.WaitForAssertion(() => Assert.False(cut.Find("button.btn-save").HasAttribute("disabled")));
            cut.Find("button.btn-save").Click();

            cut.WaitForAssertion(() => Assert.NotNull(saved));
            Assert.DoesNotContain("Please update margin", cut.Markup);

            var activated = saved!.Margins.Single(m => m.MarketTemplateId == "9484");
            Assert.True(activated.IsActive);
            Assert.False(saved.Margins.Single(m => m.MarketTemplateId == "9485").IsActive);

            // the margins are the defaults: one key of 101 at kick off, nothing else
            var margin = Assert.Single(JsonSerializer.Deserialize<List<MarketMargin>>(activated.Config)!);
            Assert.Equal(101, margin.M);
        }

        private sealed record MarginRequest(List<ConfigTemplateMargin> Margins);

        [Fact]
        public void TheKeyAndTimeNodes_AreViewOnly_AndDoNotInviteAClick()
        {
            var cut = RenderEditor();

            var nodes = cut.FindAll("a.timeline-axis-item-key-container");

            Assert.NotEmpty(nodes);
            Assert.All(nodes, node => Assert.Contains("read-only", node.ClassList));
            Assert.DoesNotContain("Click to edit key", cut.Markup);

            // a click on a node does not open the Edit Config popover
            Record.Exception(() => nodes[0].Click());
            Assert.DoesNotContain("Edit Config", cut.Markup);
        }

        [Fact]
        public void TheTemplateStillHasItsSaveAndCancelButtons_SoItCanBeRenamed()
        {
            var cut = RenderEditor();

            Assert.NotNull(cut.Find("button.btn-save"));
            Assert.NotNull(cut.Find("button.btn-cancel"));
        }

        [Fact]
        public void TheMarketsAreStillListed_NotHidden()
        {
            var cut = RenderEditor();

            Assert.Contains("4th Quarter Result", cut.Markup);
            Assert.Contains("Totals", cut.Markup);
        }

        [Fact]
        public void AfterASave_TheUserIsSentToMyTemplates_WithTheBannerTextInTheParameterThatPageReads()
        {
            _backofficeService
                .Setup(s => s.AddOrUpdateConfigTemplateMargin(It.IsAny<ConfigTemplateMarginModel>()))
                .ReturnsAsync(Ok(true));
            _backofficeService
                .Setup(s => s.AddActivityLog(It.IsAny<ActivityLogModel>()))
                .ReturnsAsync(Ok(new MessageResponse()));
            var cut = RenderEditor();

            cut.FindAll("div.market-toggle input.form-check-input")[0].Input(true);

            cut.WaitForAssertion(() => Assert.False(cut.Find("button.btn-save").HasAttribute("disabled")));
            cut.Find("button.btn-save").Click();

            var navigation = Services.GetRequiredService<NavigationManager>();
            cut.WaitForAssertion(() => Assert.Contains("/hk/odds-engine/config-templates?", navigation.Uri));

            // The list page reads only the parameter "M" (Configuration2.QueryMessage). The text used to be sent as "message",
            // so the page never showed it.
            var query = HttpUtility.ParseQueryString(new Uri(navigation.Uri).Query);
            Assert.Equal("Successfully updated", query["M"]);
            Assert.Null(query["message"]);
        }
    }
}
