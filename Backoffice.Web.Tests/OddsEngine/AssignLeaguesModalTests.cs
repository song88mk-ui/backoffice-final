using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.OddsEngine.Configuration;
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
    // BO-151: the dialog that assigns the selected leagues as Active or Restricted, changes that state, or unassigns them.
    // The server is a mock, so these cover the dialog's own flow. The save, the client scope and the activity log are
    // covered by ConfigSetStateTests in the API tests.
    public class AssignLeaguesModalTests : BunitTestContext
    {
        private readonly Mock<IBackofficeService> _backofficeService = new();
        private readonly Mock<IModalService> _modalService = new();
        private readonly List<object> _saved = new();
        private readonly List<(ConfigState State, ConfigSetLeagueStateRequest Request)> _requests = new();

        public AssignLeaguesModalTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());

            Services.AddSingleton<IConfiguration>(configuration);
            Services.AddSingleton(_modalService.Object);
            Services.AddSingleton(new Mock<IPreRenderService>().Object);
            Services.AddSingleton(new Mock<ITradingService>().Object);
            Services.AddSingleton(new Mock<IProviderMappingService>().Object);
            Services.AddSingleton(_backofficeService.Object);
            Services.AddSingleton(new Mock<IAuthenticationService>().Object);
            Services.AddSingleton(new Mock<ITableauService>().Object);
            Services.AddSingleton(new Mock<ICommonService>().Object);
            Services.AddSingleton(new TranslationState(translationService.Object));
            Services.AddSingleton(new UserState());
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                new Mock<IAuthenticationService>().Object,
                sp.GetRequiredService<NavigationManager>()));

            ServerReplies(StatusCode.Success);
        }

        private void ServerReplies(StatusCode status, string message = "Success") =>
            _backofficeService
                .Setup(s => s.SetLeagueState(It.IsAny<ConfigState>(), It.IsAny<ConfigSetLeagueStateRequest>()))
                .Callback<ConfigState, ConfigSetLeagueStateRequest>((state, request) => _requests.Add((state, request)))
                .ReturnsAsync(new BaseResponse<bool> { StatusCode = status, Message = message, Result = status == StatusCode.Success });

        private static ConfigClientViewModel League(int id, string client = "S88") => new()
        {
            Id = -1,
            ClientId = Guid.NewGuid().ToString(),
            ClientCode = client,
            ClientName = $"{client} name",
            SportId = 1,
            SportName = "Soccer",
            SegmentId = 10,
            SegmentName = "England",
            LeagueId = id,
            LeagueName = $"League {id}",
            IsChecked = true
        };

        private IRenderedComponent<AssignLeaguesModal> RenderModal(AssignLeaguesMode mode, params ConfigClientViewModel[] leagues)
        {
            var parameters = new ModalParameters();
            parameters.Add("Leagues", leagues.ToList());
            parameters.Add("Mode", mode);

            // The dialog only reads the callback, so a bare CustomModal is enough; rendering the real one needs the page.
#pragma warning disable BL0005
            var modal = new CustomModal { OnSaveClickCallback = EventCallback.Factory.Create<object>(this, alert => _saved.Add(alert)) };
#pragma warning restore BL0005

            return Render<AssignLeaguesModal>(ps => ps.AddCascadingValue(parameters).AddCascadingValue(modal));
        }

        private static AngleSharp.Dom.IElement Apply(IRenderedComponent<AssignLeaguesModal> cut) => cut.Find("button.btn-save");

        // ---- Choosing a state --------------------------------------------------------------------------------

        [Fact]
        public void Assign_PreSelectsNoState_AndApplyIsDisabledUntilOneIsChosen()
        {
            var cut = RenderModal(AssignLeaguesMode.Assign, League(1));

            Assert.False(cut.Find("#state-active").HasAttribute("checked"));
            Assert.False(cut.Find("#state-restricted").HasAttribute("checked"));
            Assert.True(Apply(cut).HasAttribute("disabled"));

            cut.Find("#state-active").Change(true);

            Assert.False(Apply(cut).HasAttribute("disabled"));
            Assert.Equal("Apply", Apply(cut).TextContent.Trim());
        }

        [Fact]
        public void TheStatesAreExplainedInPlainWords()
        {
            var cut = RenderModal(AssignLeaguesMode.Assign, League(1));

            Assert.Contains("Reviewed. The leagues stay visible to the client.", cut.Markup);
            Assert.Contains("Hidden from the client. Other clients are not affected.", cut.Markup);
        }

        [Fact]
        public void ApplyingWithoutAState_SavesNothing()
        {
            var cut = RenderModal(AssignLeaguesMode.Assign, League(1));

            Apply(cut).Click(); // disabled, so it does nothing

            Assert.Empty(_requests);
            Assert.Empty(_saved);
        }

        // ---- Saving ------------------------------------------------------------------------------------------

        [Fact]
        public void AssignAsRestricted_SendsTheLeaguesWithRestrictedSet_ThenReportsSuccessAndCloses()
        {
            var first = League(1);
            var second = League(2, "OG");
            var cut = RenderModal(AssignLeaguesMode.Assign, first, second);

            cut.Find("#state-restricted").Change(true);
            Apply(cut).Click();

            cut.WaitForAssertion(() => Assert.Single(_saved));

            var (state, request) = Assert.Single(_requests);
            Assert.Equal(ConfigState.Assigned, state);
            Assert.True(request.Restricted);
            Assert.Equal(new[] { "S88:1", "OG:2" }, request.Leagues.Select(l => $"{l.ClientCode}:{l.LeagueId}"));
            Assert.Equal(first.ClientId, request.Leagues[0].ClientId);

            var alert = Assert.IsType<AlertModel>(Assert.Single(_saved));
            Assert.Equal(AlertStatus.Success, alert.Status);
            Assert.Equal("The selected leagues were assigned as Restricted.", alert.Message);
            _modalService.Verify(m => m.Cancel(), Times.Once);
        }

        [Fact]
        public void AssignAsActive_SendsRestrictedFalse()
        {
            var cut = RenderModal(AssignLeaguesMode.Assign, League(1));

            cut.Find("#state-active").Change(true);
            Apply(cut).Click();

            cut.WaitForAssertion(() => Assert.Single(_saved));
            Assert.False(Assert.Single(_requests).Request.Restricted);
            Assert.Equal("The selected leagues were assigned as Active.", ((AlertModel)_saved[0]).Message);
        }

        [Fact]
        public void ChangeState_SaysTheStateWasChanged()
        {
            var cut = RenderModal(AssignLeaguesMode.ChangeState, League(1));

            cut.Find("#state-restricted").Change(true);
            Apply(cut).Click();

            cut.WaitForAssertion(() => Assert.Single(_saved));
            Assert.Equal(ConfigState.Assigned, Assert.Single(_requests).State);
            Assert.Equal("The state of the selected leagues was changed to Restricted.", ((AlertModel)_saved[0]).Message);
        }

        [Fact]
        public void Unassign_AsksNoStateAndIsReadyToApply_ThenSendsTheUnassignedState()
        {
            var cut = RenderModal(AssignLeaguesMode.Unassign, League(1), League(2));

            Assert.Empty(cut.FindAll("input[type=radio]"));
            Assert.Equal("Unassign", Apply(cut).TextContent.Trim());
            Assert.False(Apply(cut).HasAttribute("disabled"));

            Apply(cut).Click();

            cut.WaitForAssertion(() => Assert.Single(_saved));
            Assert.Equal(ConfigState.UnAssigned, Assert.Single(_requests).State);
            Assert.Equal("The selected leagues were unassigned.", ((AlertModel)_saved[0]).Message);
        }

        // ---- When it fails -----------------------------------------------------------------------------------

        [Fact]
        public void WhenTheSaveFails_ItShowsAPlainError_KeepsTheDialogOpen_AndNeverShowsTheApiMessage()
        {
            ServerReplies(StatusCode.Error, "Violation of PRIMARY KEY constraint 'PK_ClientLeagueConfig'");
            var cut = RenderModal(AssignLeaguesMode.Assign, League(1));

            cut.Find("#state-restricted").Change(true);
            Apply(cut).Click();

            cut.WaitForAssertion(() => Assert.Contains("The leagues could not be saved", cut.Markup));
            Assert.DoesNotContain("PK_ClientLeagueConfig", cut.Markup);
            Assert.Empty(_saved);
            _modalService.Verify(m => m.Cancel(), Times.Never);
        }

        [Fact]
        public void AfterAFailedSave_TheUserCanTryAgain_AndTheErrorGoesAway()
        {
            ServerReplies(StatusCode.Error);
            var cut = RenderModal(AssignLeaguesMode.Assign, League(1));
            cut.Find("#state-active").Change(true);
            Apply(cut).Click();
            cut.WaitForAssertion(() => Assert.Contains("The leagues could not be saved", cut.Markup));
            Assert.False(Apply(cut).HasAttribute("disabled"));

            ServerReplies(StatusCode.Success);
            Apply(cut).Click();

            cut.WaitForAssertion(() => Assert.Single(_saved));
            Assert.DoesNotContain("The leagues could not be saved", cut.Markup);
        }

        [Fact]
        public void Cancel_ClosesTheDialogWithoutSaving()
        {
            var cut = RenderModal(AssignLeaguesMode.Assign, League(1));
            cut.Find("#state-restricted").Change(true);

            cut.Find("button.btn-cancel").Click();

            _modalService.Verify(m => m.Cancel(), Times.Once);
            Assert.Empty(_requests);
        }

        // ---- The list of leagues -----------------------------------------------------------------------------

        [Fact]
        public void ThreeLeaguesAreShown_ThenShowAllRevealsTheRest()
        {
            var cut = RenderModal(AssignLeaguesMode.Assign, League(1), League(2), League(3), League(4), League(5));

            Assert.Equal(3, cut.FindAll(".league-body .border").Count);

            cut.Find("a").Click();

            Assert.Equal(5, cut.FindAll(".league-body .border").Count);
        }

        [Fact]
        public void WithThreeLeaguesOrFewer_ThereIsNoShowAllLink()
        {
            var cut = RenderModal(AssignLeaguesMode.Assign, League(1), League(2), League(3));

            Assert.Empty(cut.FindAll("a"));
        }
    }
}
