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
    // OE Config > Assigned is read-only (BO-128): the odds come from the provider as is, so no template (margin)
    // can be opened, selected or changed from it. The Unassigned tab keeps assigning templates.
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

            var authContext = this.AddAuthorization();
            authContext.SetAuthorized("test-user");
            authContext.SetPolicies("sbc-config-canedit");

            // No rows: the page shows "No Record Found", which is enough to drive the selection bar and its buttons.
            _backofficeService
                .Setup(s => s.GetConfigClient(It.IsAny<ConfigState>(), It.IsAny<ConfigTemplateLeagueRequest>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BaseResponse<ConfigTemplateLeagueResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ConfigTemplateLeagueResponse { ClientConfigs = new List<ConfigClientModel>(), MetaData = new MetaData() }
                });
        }

        private static ConfigClientViewModel League(long prematchTemplateId = -1, long liveTemplateId = -1) => new()
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
            LiveTemplateName = liveTemplateId > 0 ? "Soccer [Live][Tier 1]" : ""
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
        public void AssignedRow_ShowsTheTemplateNamesAsMutedText_NotAsLinks()
        {
            var cut = Render<ConfigStateRow>(ps => ps
                .Add(p => p.State, ConfigState.Assigned)
                .Add(p => p.Config, League(prematchTemplateId: 5, liveTemplateId: 7)));

            Assert.Empty(cut.FindAll("a"));
            Assert.Equal(
                new[] { "Soccer [Prematch][Tier 1]", "Soccer [Live][Tier 1]" },
                cut.FindAll("label.text-muted").Select(l => l.TextContent.Trim()));
        }

        [Fact]
        public void AssignedRow_HasADisabledCheckbox()
        {
            var cut = Render<ConfigStateRow>(ps => ps
                .Add(p => p.State, ConfigState.Assigned)
                .Add(p => p.Config, League(prematchTemplateId: 5, liveTemplateId: 7)));

            Assert.True(cut.Find("input[type=checkbox]").HasAttribute("disabled"));
        }

        [Fact]
        public void UnassignedRow_KeepsAnEnabledCheckbox_ThatRaisesTheSelectionEvent()
        {
            var league = League();
            ConfigClientViewModel? selected = null;

            var cut = Render<ConfigStateRow>(ps => ps
                .Add(p => p.State, ConfigState.UnAssigned)
                .Add(p => p.Config, league)
                .Add(p => p.ItemSelectionChangedEvent, EventCallback.Factory.Create<ConfigClientViewModel>(this, c => selected = c)));

            var checkbox = cut.Find("input[type=checkbox]");
            Assert.False(checkbox.HasAttribute("disabled"));

            checkbox.Change(true);

            Assert.Same(league, selected);
            Assert.True(league.IsChecked);
        }

        [Fact]
        public void AssignedTab_DisablesEveryButtonOfTheSelectionBar()
        {
            var cut = RenderPage(ConfigState.Assigned);

            foreach (var selector in new[] { ".btn-selection-all", ".btn-selection-clear", ".btn-selection-modify" })
                Assert.True(cut.Find(selector).HasAttribute("disabled"), selector);
        }

        [Fact]
        public void UnassignedTab_KeepsTheButtonsOfTheSelectionBarEnabled()
        {
            var cut = RenderPage(ConfigState.UnAssigned);

            foreach (var selector in new[] { ".btn-selection-all", ".btn-selection-clear", ".btn-selection-modify" })
                Assert.False(cut.Find(selector).HasAttribute("disabled"), selector);
        }

        [Fact]
        public async Task ChangeTemplate_OnTheAssignedTab_OpensNoModal()
        {
            var cut = RenderPage(ConfigState.Assigned);

            await cut.InvokeAsync(() => cut.Instance.HandleEditTemplateClick());

            Assert.Empty(_modalService.Opened);
        }

        [Fact]
        public async Task AssignTemplate_OnTheUnassignedTab_StillOpensTheModal()
        {
            var cut = RenderPage(ConfigState.UnAssigned);

            await cut.InvokeAsync(() => cut.Instance.HandleEditTemplateClick());

            var opened = Assert.Single(_modalService.Opened);
            Assert.Equal(typeof(ModifyLeagueTemplateModal), opened.Component);
            Assert.Equal("Assign Template", opened.Title);
        }

        [Fact]
        public async Task SwitchingTabs_DropsASelectionMadeOnTheOtherTab()
        {
            var cut = RenderPage(ConfigState.UnAssigned);
            await cut.InvokeAsync(() => cut.Instance.HandleConfigSelectionChange(
                new ConfigClientViewModel { IsChecked = true, ClientCode = "10BET", SportId = 1, LeagueId = 100 }));
            Assert.Single(JSInterop.Invocations[ToggleSelectionBar]);

            cut.Render(ps => ps.Add(p => p.State, ConfigState.Assigned));

            // The selection bar was showing, so it is toggled away on the way to the Assigned tab.
            Assert.Equal(2, JSInterop.Invocations[ToggleSelectionBar].Count);

            cut.Render(ps => ps.Add(p => p.State, ConfigState.UnAssigned));
            await cut.InvokeAsync(() => cut.Instance.HandleEditTemplateClick());

            var opened = Assert.Single(_modalService.Opened);
            Assert.Empty(opened.Parameters.TryGet<List<ConfigClientViewModel>>("Leagues")!);
        }
    }
}
