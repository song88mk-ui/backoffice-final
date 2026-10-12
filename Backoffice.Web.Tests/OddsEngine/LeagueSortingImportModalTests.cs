using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.OddsEngine.LeagueSorting;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Backoffice.Web.Tests.OddsEngine
{
    // Component tests for the League Sorting import dialog (BO-122): choose a file, review what the server
    // validated, import, and the ways each step can fail. The server is a mock, so these cover the dialog's
    // own flow; the merge rules are covered by OddstableLeagueImportPlannerTests in the API tests.
    public class LeagueSortingImportModalTests : BunitTestContext
    {
        private const string Header = "ClientCode,SportId,SegmentId,SegmentName,LeagueId,LeagueName,SortOrder";

        private readonly Mock<IBackofficeService> _backofficeService = new();
        private readonly List<ImportClientOddstableLeagueSettingRequest> _requests = new();
        private int _saved;

        public LeagueSortingImportModalTests()
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
            Services.AddSingleton(userState);
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                new Mock<IAuthenticationService>().Object,
                sp.GetRequiredService<NavigationManager>()));
        }

        private static ImportOddstableLeagueRowResult RowResult(int rowNo, int segmentId, string segmentName, int leagueId, string leagueName, int sortOrder, bool matched, string reason = "") =>
            new ImportOddstableLeagueRowResult
            {
                RowNo = rowNo,
                SegmentId = segmentId,
                SegmentName = segmentName,
                LeagueId = leagueId,
                LeagueName = leagueName,
                SortOrder = sortOrder,
                Matched = matched,
                Reason = reason
            };

        // Two matched leagues, one skipped, one league of the client kept after them.
        private static ImportClientOddstableLeagueSettingResponse Reviewed(bool hasChanges = true) =>
            new ImportClientOddstableLeagueSettingResponse
            {
                StatusCode = StatusCode.Success,
                Message = "The file is valid.",
                HasChanges = hasChanges,
                MatchedCount = 2,
                UnmatchedCount = 1,
                RetainedCount = 1,
                ResultCount = 3,
                Rows = new List<ImportOddstableLeagueRowResult>
                {
                    RowResult(2, 10, "England", 100, "Premier League", 1, true),
                    RowResult(3, 20, "Spain", 200, "La Liga", 2, true),
                    RowResult(4, 0, "", 999, "", 3, false, "The league is not available for this sport.")
                }
            };

        private static ImportClientOddstableLeagueSettingResponse Imported()
        {
            var response = Reviewed();
            response.Committed = true;
            response.Message = "Success! Client League Order has been successfully imported.";
            return response;
        }

        private static BaseResponse<ImportClientOddstableLeagueSettingResponse> Reply(ImportClientOddstableLeagueSettingResponse result) =>
            new BaseResponse<ImportClientOddstableLeagueSettingResponse> { StatusCode = StatusCode.Success, Message = "Success", Result = result };

        private void ServerReplies(Func<ImportClientOddstableLeagueSettingRequest, BaseResponse<ImportClientOddstableLeagueSettingResponse>> reply)
        {
            _backofficeService
                .Setup(s => s.ImportClientOddstableLeagueSettings(It.IsAny<ImportClientOddstableLeagueSettingRequest>()))
                .Callback<ImportClientOddstableLeagueSettingRequest>(r => _requests.Add(r))
                .ReturnsAsync((ImportClientOddstableLeagueSettingRequest r) => reply(r));
        }

        private IRenderedComponent<ImportModal> Render()
        {
            var parameters = new ModalParameters();
            parameters.Add("ClientCode", "QTE-A");
            parameters.Add("ClientName", "Client A");
            parameters.Add("SportId", 1);
            parameters.Add("SportName", "Football");
            parameters.Add("CurrentLeagueCount", 2);

            // The dialog only reads the callback, so a bare CustomModal is enough; rendering the real one needs the page.
#pragma warning disable BL0005
            var modal = new CustomModal { OnSaveClickCallback = EventCallback.Factory.Create<object>(this, _ => _saved++) };
#pragma warning restore BL0005

            return Render<ImportModal>(ps => ps.AddCascadingValue(parameters).AddCascadingValue(modal));
        }

        private static void Upload(IRenderedComponent<ImportModal> cut, string text) =>
            cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(text, "league-sorting.csv"));

        private static string ValidFile() =>
            string.Join("\r\n", Header, "QTE-A,1,10,England,100,Premier League,1", "QTE-A,1,20,Spain,200,La Liga,2", "QTE-A,1,0,Nowhere,999,Unknown,3");

        private static AngleSharp.Dom.IElement ButtonLabelled(IRenderedComponent<ImportModal> cut, string label) =>
            cut.FindAll("button").First(b => b.TextContent.Trim() == label);

        private static List<string> Counts(IRenderedComponent<ImportModal> cut) =>
            cut.FindAll(".ls-import-count").Select(c => c.TextContent.Trim()).ToList();

        [Fact]
        public void Opens_OnTheFileStep_ShowingTheTarget_AndNoImportButton()
        {
            var cut = Render();

            Assert.Equal(new[] { "Client A", "Football", "2" }, cut.FindAll(".ls-import-value").Select(v => v.TextContent.Trim()));
            Assert.NotNull(cut.FindComponent<InputFile>());
            Assert.Contains(cut.FindAll("button"), b => b.TextContent.Trim() == "Cancel");
            Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Import");
        }

        [Fact]
        public void ChoosingAValidFile_ValidatesItWithoutCommitting_AndShowsTheReview()
        {
            ServerReplies(_ => Reply(Reviewed()));
            var cut = Render();

            Upload(cut, ValidFile());

            cut.WaitForAssertion(() => Assert.Equal(new[] { "2", "1", "1", "3" }, Counts(cut)));

            var request = Assert.Single(_requests);
            Assert.False(request.Commit);
            Assert.Equal(("QTE-A", 1, "Football", "test-user"), (request.ClientCode, request.SportId, request.SportName, request.UserId));
            Assert.Equal(new[] { (2, 10, 100, 1), (3, 20, 200, 2), (4, 0, 999, 3) }, request.Rows.Select(r => (r.RowNo, r.SegmentId, r.LeagueId, r.SortOrder)));

            Assert.Contains("England > Premier League", cut.Markup);
            Assert.Contains("Spain > La Liga", cut.Markup);
            Assert.Contains("The league is not available for this sport.", cut.Markup);
            Assert.False(ButtonLabelled(cut, "Import").HasAttribute("disabled"));
            Assert.Equal(0, _saved);
        }

        [Fact]
        public void Importing_SendsTheSameRowsWithCommit_ThenShowsTheResult_AndRefreshesThePage()
        {
            ServerReplies(r => Reply(r.Commit ? Imported() : Reviewed()));
            var cut = Render();
            Upload(cut, ValidFile());
            cut.WaitForAssertion(() => ButtonLabelled(cut, "Import"));

            ButtonLabelled(cut, "Import").Click();

            cut.WaitForAssertion(() => Assert.Contains("successfully imported", cut.Markup));
            Assert.Equal(new[] { false, true }, _requests.Select(r => r.Commit));
            Assert.Equal(_requests[0].Rows.Select(r => r.LeagueId), _requests[1].Rows.Select(r => r.LeagueId));
            Assert.Equal(1, _saved);
            Assert.Contains("The league is not available for this sport.", cut.Markup);
            Assert.Contains(cut.FindAll("button"), b => b.TextContent.Trim() == "Close");
            Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Import");
        }

        [Fact]
        public void AFileForAnotherClient_IsRefusedWithTheRow_AndNothingIsSentToTheServer()
        {
            ServerReplies(_ => Reply(Reviewed()));
            var cut = Render();

            Upload(cut, string.Join("\r\n", Header, "QTE-B,1,10,England,100,Premier League,1"));

            cut.WaitForAssertion(() => Assert.Contains("Row 2: ClientCode 'QTE-B' is not the selected client (QTE-A).", cut.Markup));
            Assert.Empty(_requests);
            Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Import");
        }

        [Fact]
        public void ProblemsFoundByTheServer_AreListed_AndImportingIsNotPossible()
        {
            ServerReplies(_ => Reply(new ImportClientOddstableLeagueSettingResponse
            {
                StatusCode = StatusCode.Error,
                Message = "The file cannot be imported. Nothing was saved.",
                Errors = new List<string> { "Row 3: league 100 is already listed in row 2." }
            }));
            var cut = Render();

            Upload(cut, ValidFile());

            cut.WaitForAssertion(() => Assert.Contains("Row 3: league 100 is already listed in row 2.", cut.Markup));
            Assert.Contains("The file cannot be imported. Nothing was saved.", cut.Markup);
            Assert.True(ButtonLabelled(cut, "Import").HasAttribute("disabled"));
        }

        [Fact]
        public void AFileThatChangesNothing_SaysSo_AndImportingIsNotPossible()
        {
            ServerReplies(_ => Reply(Reviewed(hasChanges: false)));
            var cut = Render();

            Upload(cut, ValidFile());

            cut.WaitForAssertion(() => Assert.Contains("already in this order", cut.Markup));
            Assert.True(ButtonLabelled(cut, "Import").HasAttribute("disabled"));
        }

        [Fact]
        public void WhenTheServerCannotBeReached_ShowsTheErrorAndStaysOnTheFileStep()
        {
            ServerReplies(_ => new BaseResponse<ImportClientOddstableLeagueSettingResponse> { StatusCode = StatusCode.Error, Message = "Error (Bad Gateway)" });
            var cut = Render();

            Upload(cut, ValidFile());

            cut.WaitForAssertion(() => Assert.Contains("Error (Bad Gateway)", cut.Markup));
            Assert.NotNull(cut.FindComponent<InputFile>());
            Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Import");
        }

        [Fact]
        public void WhenTheCommitFindsNothingToSave_StaysOnTheReview_WithoutRefreshingThePage()
        {
            ServerReplies(r => Reply(r.Commit ? Reviewed(hasChanges: false) : Reviewed()));
            var cut = Render();
            Upload(cut, ValidFile());
            cut.WaitForAssertion(() => ButtonLabelled(cut, "Import"));

            ButtonLabelled(cut, "Import").Click();

            cut.WaitForAssertion(() => Assert.Contains("already in this order", cut.Markup));
            Assert.Equal(0, _saved);
            Assert.True(ButtonLabelled(cut, "Import").HasAttribute("disabled"));
        }

        [Fact]
        public void OtherFile_GoesBackToTheFileStep_AndClearsTheReview()
        {
            ServerReplies(_ => Reply(Reviewed()));
            var cut = Render();
            Upload(cut, ValidFile());
            cut.WaitForAssertion(() => ButtonLabelled(cut, "Other file"));

            ButtonLabelled(cut, "Other file").Click();

            cut.WaitForAssertion(() => Assert.NotNull(cut.FindComponent<InputFile>()));
            Assert.Empty(cut.FindAll(".ls-import-count"));
            Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Import");
        }
    }
}
