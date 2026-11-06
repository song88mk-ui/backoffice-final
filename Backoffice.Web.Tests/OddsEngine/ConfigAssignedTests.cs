using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.OddsEngine.Configuration;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Config;
using Backoffice.Web.Pages.OddsEngine;
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
    // OE Config > Assigned and Unassigned (BO-151): no templates or margins any more (BO-128 made the odds come from
    // the provider as is). A league is Unassigned (not reviewed yet) or Assigned to the client as Active or Restricted.
    // The page lists them with their state and opens one dialog to assign, change the state, or unassign.
    public class ConfigAssignedTests : BunitTestContext
    {
        private const string ToggleSelectionBar = "showSelectedLeagueCount";

        // CustomModal casts the injected IModalService to ModalService, so a Moq mock cannot stand in for it. This
        // subclass is a real ModalService that also records what the page asks to open.
        private sealed class RecordingModalService : ModalService, IModalService
        {
            public RecordingModalService(TranslationState translationState) : base(translationState) { }

            public List<(Type Component, string Title, ModalParameters Parameters)> Opened { get; } = new();

            void IModalService.Open<T>(string title, ModalParameters parameters) => Opened.Add((typeof(T), title, parameters));

            void IModalService.Open<T>(ModalParameters parameters, ModalOptions options) => Opened.Add((typeof(T), string.Empty, parameters));

            void IModalService.Open<T>(string title, ModalParameters parameters, ModalOptions options) => Opened.Add((typeof(T), title, parameters));

            void IModalService.Open(Type contentComponent, string title, ModalParameters parameters, ModalOptions options) => Opened.Add((contentComponent, title, parameters));
        }

        private readonly Mock<IBackofficeService> _backofficeService = new();
        private readonly RecordingModalService _modalService;
        private readonly BunitAuthorizationContext _authContext;

        public ConfigAssignedTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;

            // ConfigAssigned and ConfigStateRow sit under Backoffice.Web, so they pick up every @inject in
            // _Imports.razor; bUnit's DI throws on render unless all of them are registered.
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

            _modalService = new RecordingModalService(translationState);

            Services.AddSingleton<IConfiguration>(configuration);
            Services.AddSingleton<IModalService>(_modalService);
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

            _authContext = this.AddAuthorization();
            _authContext.SetAuthorized("test-user");
            _authContext.SetPolicies("sbc-config-canedit");

            // No rows: the page shows "No Record Found", which is enough to drive the selection bar and its buttons.
            _backofficeService
                .Setup(s => s.GetConfigClient(It.IsAny<ConfigState>(), It.IsAny<ConfigTemplateLeagueRequest>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BaseResponse<ConfigTemplateLeagueResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ConfigTemplateLeagueResponse { ClientConfigs = new List<ConfigClientModel>(), MetaData = new MetaData() }
                });
        }

        private static ConfigClientViewModel League(long prematchTemplateId = -1, long liveTemplateId = -1, bool isRestricted = false) => new()
        {
            ClientCode = "10BET",
            ClientName = "10Bet Crypto",
            SportId = 1,
            SportName = "Soccer",
            SegmentId = 10,
            SegmentName = "England",
            LeagueId = 100,
            LeagueName = "Premier League",
            PrematchTemplateId = prematchTemplateId,
            PrematchTemplateName = prematchTemplateId > 0 ? "Soccer [Prematch][Tier 1]" : "",
            LiveTemplateId = liveTemplateId,
            LiveTemplateName = liveTemplateId > 0 ? "Soccer [Live][Tier 1]" : "",
            IsRestricted = isRestricted
        };

        private IRenderedComponent<ConfigAssigned> RenderPage(ConfigState state)
        {
            var filter = new ConfigContentFilterModel
            {
                Clients = new List<SelectListItemWithCheckboxModel<string>>(),
                Sports = new List<SelectListItemModel<string>> { new() { Value = "1", Name = "Soccer" } },
                Scopes = new List<SelectListItemModel<string>>()
            };

            var cut = Render<ConfigAssigned>(ps => ps
                .Add(p => p.State, state)
                .Add(p => p.FilterModel, filter));
            cut.WaitForAssertion(() => Assert.Contains("No Record Found", cut.Markup));

            return cut;
        }

        [Fact]
        public void AssignedRow_ShowsItsStateAsABadge()
        {
            var active = Render<ConfigStateRow>(ps => ps.Add(p => p.State, ConfigState.Assigned).Add(p => p.Config, League()));
            var restricted = Render<ConfigStateRow>(ps => ps.Add(p => p.State, ConfigState.Assigned).Add(p => p.Config, League(isRestricted: true)));

            var activeBadge = active.Find("span.badge");
            var restrictedBadge = restricted.Find("span.badge");

            Assert.Equal("Active", activeBadge.TextContent.Trim());
            Assert.Contains("bg-success", activeBadge.ClassList);
            Assert.Equal("Restricted", restrictedBadge.TextContent.Trim());
            Assert.Contains("bg-danger", restrictedBadge.ClassList);
        }

        [Fact]
        public void TheRows_HaveNoTemplateColumns()
        {
            var assigned = Render<ConfigStateRow>(ps => ps.Add(p => p.State, ConfigState.Assigned).Add(p => p.Config, League(prematchTemplateId: 5, liveTemplateId: 7)));
            var unassigned = Render<ConfigStateRow>(ps => ps.Add(p => p.State, ConfigState.UnAssigned).Add(p => p.Config, League()));

            Assert.DoesNotContain("template", assigned.Markup.Replace("template-name-italic", string.Empty), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("No template assigned", unassigned.Markup);
            Assert.Equal(1, assigned.FindAll("td").Count(td => td.QuerySelector("span.badge") != null));
        }

        [Fact]
        public void UnassignedRow_SaysTheLeagueIsNotReviewedYet()
        {
            var cut = Render<ConfigStateRow>(ps => ps.Add(p => p.State, ConfigState.UnAssigned).Add(p => p.Config, League()));

            Assert.Equal("Not reviewed", cut.Find("td span.template-name-italic").TextContent.Trim());
            Assert.Empty(cut.FindAll("span.badge"));
        }

        [Theory]
        [InlineData(ConfigState.Assigned)]
        [InlineData(ConfigState.UnAssigned)]
        public void TheCheckbox_IsEnabledOnBothTabs_AndRaisesTheSelectionEvent(ConfigState state)
        {
            var league = League();
            ConfigClientViewModel? selected = null;

            var cut = Render<ConfigStateRow>(ps => ps
                .Add(p => p.State, state)
                .Add(p => p.Config, league)
                .Add(p => p.ItemSelectionChangedEvent, EventCallback.Factory.Create<ConfigClientViewModel>(this, c => selected = c)));

            var checkbox = cut.Find("input[type=checkbox]");
            Assert.False(checkbox.HasAttribute("disabled"));

            checkbox.Change(true);

            Assert.Same(league, selected);
            Assert.True(league.IsChecked);
        }

        [Fact]
        public void TheTableHeader_HasAStateColumn_AndNoTemplateColumns()
        {
            var cut = RenderPage(ConfigState.Assigned);

            var headers = cut.FindAll("thead th").Select(th => th.TextContent.Trim()).Where(t => t.Length > 0).ToList();

            Assert.Equal(new[] { "No.", "Client name", "Sport", "League Name", "State" }, headers);
        }

        [Fact]
        public void AssignedTab_EnablesTheSelectionButtons_AndOffersUnassign()
        {
            var cut = RenderPage(ConfigState.Assigned);

            foreach (var selector in new[] { ".btn-selection-all", ".btn-selection-clear:not(.btn-selection-unassign)", ".btn-selection-unassign", ".btn-selection-modify" })
                Assert.False(cut.Find(selector).HasAttribute("disabled"), selector);

            Assert.Equal("Unassign", cut.Find(".btn-selection-unassign").TextContent.Trim());
            Assert.Equal("Change state", cut.Find(".btn-selection-modify").TextContent.Trim());
        }

        [Fact]
        public void UnassignedTab_OffersAssign_AndNoUnassign()
        {
            var cut = RenderPage(ConfigState.UnAssigned);

            Assert.Empty(cut.FindAll(".btn-selection-unassign"));
            Assert.Equal("Assign", cut.Find(".btn-selection-modify").TextContent.Trim());
            Assert.False(cut.Find(".btn-selection-modify").HasAttribute("disabled"));
        }

        // ---- The dialog it opens -----------------------------------------------------------------------------

        private static async Task Select(IRenderedComponent<ConfigAssigned> cut, params int[] leagueIds)
        {
            foreach (var id in leagueIds)
                await cut.InvokeAsync(() => cut.Instance.HandleConfigSelectionChange(
                    new ConfigClientViewModel { IsChecked = true, ClientCode = "10BET", SportId = 1, LeagueId = id }));
        }

        [Fact]
        public async Task Assign_OnTheUnassignedTab_OpensTheDialog_WithTheSelectedLeagues()
        {
            var cut = RenderPage(ConfigState.UnAssigned);
            await Select(cut, 100, 101);

            await cut.InvokeAsync(() => cut.Instance.HandleAssignClick());

            var opened = Assert.Single(_modalService.Opened);
            Assert.Equal(typeof(AssignLeaguesModal), opened.Component);
            Assert.Equal("Assign Leagues", opened.Title);
            Assert.Equal(AssignLeaguesMode.Assign, opened.Parameters.TryGet<AssignLeaguesMode>("Mode"));
            Assert.Equal(new[] { 100, 101 }, opened.Parameters.TryGet<List<ConfigClientViewModel>>("Leagues")!.Select(l => l.LeagueId));
        }

        [Fact]
        public async Task ChangeState_OnTheAssignedTab_OpensTheDialog_ToChangeTheState()
        {
            var cut = RenderPage(ConfigState.Assigned);
            await Select(cut, 100);

            await cut.InvokeAsync(() => cut.Instance.HandleAssignClick());

            var opened = Assert.Single(_modalService.Opened);
            Assert.Equal(typeof(AssignLeaguesModal), opened.Component);
            Assert.Equal("Change State", opened.Title);
            Assert.Equal(AssignLeaguesMode.ChangeState, opened.Parameters.TryGet<AssignLeaguesMode>("Mode"));
        }

        [Fact]
        public async Task Unassign_OnTheAssignedTab_OpensTheDialog_ToUnassign()
        {
            var cut = RenderPage(ConfigState.Assigned);
            await Select(cut, 100);

            await cut.InvokeAsync(() => cut.Instance.HandleUnassignClick());

            var opened = Assert.Single(_modalService.Opened);
            Assert.Equal("Unassign Leagues", opened.Title);
            Assert.Equal(AssignLeaguesMode.Unassign, opened.Parameters.TryGet<AssignLeaguesMode>("Mode"));
        }

        // BO-155: the history of the assigns, state changes and unassigns. It is read only, so it is on both tabs and for a
        // user who cannot edit.
        [Theory]
        [InlineData(ConfigState.Assigned, "sbc-config-canedit")]
        [InlineData(ConfigState.UnAssigned, "sbc-config-canedit")]
        [InlineData(ConfigState.Assigned, "sbc-config")]
        [InlineData(ConfigState.UnAssigned, "sbc-config")]
        public void TheLogsButton_OpensTheLeagueLogs_WithTheClientsAndSportsOfThePage(ConfigState state, string policy)
        {
            _authContext.SetPolicies(policy);
            var cut = RenderPage(state);

            cut.Find("a.btn-logs").Click();

            var opened = Assert.Single(_modalService.Opened);
            Assert.Equal(typeof(ViewLeagueLogs), opened.Component);
            Assert.Equal("View Logs", opened.Title);
            Assert.Same(cut.Instance.FilterModel.Clients, opened.Parameters.TryGet<List<SelectListItemWithCheckboxModel<string>>>("Clients"));
            Assert.Same(cut.Instance.FilterModel.Sports, opened.Parameters.TryGet<List<SelectListItemModel<string>>>("Sports"));
        }

        [Fact]
        public async Task WithNothingSelected_NoDialogOpens()
        {
            var cut = RenderPage(ConfigState.UnAssigned);

            await cut.InvokeAsync(() => cut.Instance.HandleAssignClick());
            await cut.InvokeAsync(() => cut.Instance.HandleUnassignClick());

            Assert.Empty(_modalService.Opened);
        }

        [Fact]
        public async Task SwitchingTabs_DropsASelectionMadeOnTheOtherTab()
        {
            var cut = RenderPage(ConfigState.UnAssigned);
            await Select(cut, 100);
            Assert.Single(JSInterop.Invocations[ToggleSelectionBar]);

            cut.Render(ps => ps.Add(p => p.State, ConfigState.Assigned));

            // The selection bar was showing, so it is toggled away on the way to the Assigned tab.
            Assert.Equal(2, JSInterop.Invocations[ToggleSelectionBar].Count);

            await cut.InvokeAsync(() => cut.Instance.HandleAssignClick());

            // Nothing is selected on this tab, so no dialog opens with the leagues of the other one.
            Assert.Empty(_modalService.Opened);
        }

        // After a save the page reloads the list, hides the selection bar (a JS call) and then draws. The page only draws while its
        // render flag is set, and OnAfterRender clears it. In Blazor Server OnAfterRender runs once the browser has confirmed the
        // spinner draw, which can land during that JS call, and the final draw was skipped: the spinner stayed up over a loaded list.
        [Fact]
        public async Task AfterASave_TheReloadedList_IsDrawn_EvenIfTheRenderFlagIsClearedWhileTheSelectionBarIsHidden()
        {
            var cut = RenderPage(ConfigState.Assigned);
            await Select(cut, 100);

            // The reload after the save finds a league that was not there before.
            _backofficeService
                .Setup(s => s.GetConfigClient(It.IsAny<ConfigState>(), It.IsAny<ConfigTemplateLeagueRequest>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BaseResponse<ConfigTemplateLeagueResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ConfigTemplateLeagueResponse
                    {
                        ClientConfigs = new List<ConfigClientModel>
                        {
                            new() { ClientCode = "10BET", ClientName = "10Bet Crypto", SportId = 1, SportName = "Soccer", SegmentId = 10, SegmentName = "England", LeagueId = 100, LeagueName = "Premier League" }
                        },
                        MetaData = new MetaData()
                    }
                });

            // Hiding the selection bar stays pending until the test lets it finish.
            var hideBar = JSInterop.SetupVoid(ToggleSelectionBar);

            var save = cut.InvokeAsync(() => cut.Instance.HandleModalSave(new AlertModel { Status = AlertStatus.Success, Message = "The selected leagues were unassigned." }));
            cut.WaitForAssertion(() => Assert.Equal(2, JSInterop.Invocations[ToggleSelectionBar].Count));

            // The list has loaded and the page is waiting for the browser. The render flag is cleared here, as the late OnAfterRender
            // does; a parent pass with nothing changed clears it the same way.
            cut.Render(ps => ps.Add(p => p.State, ConfigState.Assigned));
            hideBar.SetVoidResult();
            await save;

            cut.WaitForAssertion(() => Assert.Contains("Premier League", cut.Markup));
            Assert.DoesNotContain("No Record Found", cut.Markup);
        }
    }
}
