using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
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
    // BO-155: the Logs dialog of Config > Assigned and Unassigned. It reads the league rows that Backoffice.API writes to
    // the activity log page SBC.OE.Config.Leagues (BO-151): payload "{clientCode}.{sportId}.{segmentId}.{leagueId}", the
    // old and new state in From and To. The row holds ids only, so the league name comes from the league lookup.
    public class ViewLeagueLogsTests : BunitTestContext
    {
        private record Call(string Page, string Payload, string SearchText, List<string> Terms, int CurrentPage, int PageSize);

        private readonly Mock<IBackofficeService> _backofficeService = new();
        private readonly Mock<IModalService> _modalService = new();
        private readonly UserState _userState = new() { TimeZoneOffset = TimeSpan.FromHours(8) };
        private readonly List<Call> _calls = new();

        public ViewLeagueLogsTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;

            // Every component in Backoffice.Web receives all _Imports.razor @inject services, so the fixture registers the full set.
            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

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
            Services.AddSingleton(_userState);
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                new Mock<IAuthenticationService>().Object,
                sp.GetRequiredService<NavigationManager>()));

            ServeLogs(new List<ActivityLogModel>());
            ServeLeagues(7, "Premier League");
        }

        private static readonly DateTime SavedAt = new(2026, 11, 5, 22, 30, 0, DateTimeKind.Utc);

        private static ActivityLogModel Row(string payload, string activity = "Create", string from = "Unassigned", string to = "Active", string user = "trader") => new()
        {
            Page = "SBC.OE.Config.Leagues",
            Payload = payload,
            Activity = activity,
            From = from,
            To = to,
            UserId = "u-1",
            UserName = user,
            TimeStamp = SavedAt
        };

        private void ServeLogs(List<ActivityLogModel> logs)
        {
            _backofficeService
                .Setup(s => s.GetActivityLogs(It.IsAny<ActivityLogRequest>()))
                .Callback<ActivityLogRequest>(r => _calls.Add(new Call(r.Page, r.Payload, r.SearchText, r.PayloadSearchTerms, r.MetaData.CurrentPage, r.MetaData.PageSize)))
                .ReturnsAsync(new BaseResponse<ActivityLogResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ActivityLogResponse
                    {
                        ActivityLogs = logs,
                        MetaData = new MetaData { CurrentPage = 1, PageSize = 20, TotalCount = logs.Count, TotalPages = 1 }
                    }
                });
        }

        private void ServeLeagues(int leagueId, string leagueName)
        {
            _backofficeService
                .Setup(s => s.GetLeagueLookup(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<LanguageCode>()))
                .ReturnsAsync(new BaseResponse<LeagueLookupResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new LeagueLookupResponse
                    {
                        LeagueLookup = new List<LeagueLookupResponse.LeagueLookupModel>
                        {
                            new() { SportId = 1, SegmentId = 10, LeagueId = leagueId, LeagueName = leagueName }
                        }
                    }
                });
        }

        private IRenderedComponent<ViewLeagueLogs> Render()
        {
            var parameters = new ModalParameters();
            parameters.Add("Clients", new List<SelectListItemWithCheckboxModel<string>>
            {
                new() { Value = "All", Name = "All Clients", IsChecked = true },
                new() { Value = "S88", Name = "Song 88", IsChecked = true },
                new() { Value = "OG", Name = "OG Sports", IsChecked = true }
            });
            parameters.Add("Sports", new List<SelectListItemModel<string>>
            {
                new() { Value = "1", Name = "Soccer" },
                new() { Value = "2", Name = "Basketball" }
            });

            var cut = Render<ViewLeagueLogs>(ps => ps.AddCascadingValue(parameters));
            cut.WaitForState(() => LogRows(cut).Count > 0 && cut.FindAll("table.table-logs .spinner-border").Count == 0);

            return cut;
        }

        // The rows of the logs table only: the date range picker draws calendar tables of its own.
        private static IReadOnlyList<AngleSharp.Dom.IElement> LogRows(IRenderedComponent<ViewLeagueLogs> cut) =>
            cut.FindAll("table.table-logs tbody tr");

        private static List<string> Cells(IRenderedComponent<ViewLeagueLogs> cut, int row = 0) =>
            LogRows(cut)[row].QuerySelectorAll("td").Select(td => td.TextContent.Trim()).ToList();

        [Fact]
        public void ItReadsTheLeaguesPage_ForEveryClientAndSport_OnTheFirstPage()
        {
            Render();

            var call = Assert.Single(_calls);
            Assert.Equal("SBC.OE.Config.Leagues", call.Page);
            Assert.Equal("%%.%%.%%.%%", call.Payload);
            Assert.Equal(1, call.CurrentPage);
            Assert.Equal(20, call.PageSize);
        }

        [Fact]
        public void TheClientList_HasOneAllClientsEntry_NotTheOneFromTheConfigPage()
        {
            var cut = Render();

            var options = cut.FindAll("#league-log-client option").Select(o => o.TextContent.Trim()).ToList();

            Assert.Equal(new[] { "All Clients", "Song 88", "OG Sports" }, options);
        }

        [Fact]
        public void ARow_ShowsTheClient_TheSport_TheLeagueName_TheChange_TheStates_AndTheUser()
        {
            ServeLogs(new List<ActivityLogModel> { Row("S88.1.10.7") });

            var cut = Render();

            // 22:30 UTC on 5 November is 06:30 on the 6th in a UTC+8 zone (BO-105)
            Assert.Equal(new[] { "06/11/26 06:30", "Song 88 (S88)", "Soccer", "Premier League", "Assigned", "Unassigned", "Active", "trader" }, Cells(cut));
        }

        // On DEV every row written by SetConfigState has a user id and a NULL user name (the API reads a UserName claim that
        // the Auth tokens do not have), so a missing name is shown as a dash.
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ARowWithNoUserName_ShowsADash(string? user)
        {
            ServeLogs(new List<ActivityLogModel> { Row("S88.1.10.7", user: user!) });

            var cut = Render();

            Assert.Equal("-", Cells(cut)[7]);
        }

        [Fact]
        public void TheLeagueNames_AreReadOncePerSportAndSegment_NotOncePerRow()
        {
            ServeLogs(new List<ActivityLogModel> { Row("S88.1.10.7"), Row("OG.1.10.7"), Row("S88.1.10.9") });

            var cut = Render();

            Assert.Equal(3, LogRows(cut).Count);
            _backofficeService.Verify(s => s.GetLeagueLookup(1, 10, It.IsAny<LanguageCode>()), Times.Once);
        }

        [Fact]
        public void ALeagueTheLookupDoesNotKnow_IsShownByItsId()
        {
            ServeLogs(new List<ActivityLogModel> { Row("S88.1.10.99") });

            var cut = Render();

            Assert.Equal("League 99", Cells(cut)[3]);
        }

        [Fact]
        public void WhenTheLookupFails_TheRowsStillShow_WithTheLeagueId()
        {
            ServeLogs(new List<ActivityLogModel> { Row("S88.1.10.7") });
            _backofficeService
                .Setup(s => s.GetLeagueLookup(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<LanguageCode>()))
                .ReturnsAsync(new BaseResponse<LeagueLookupResponse> { StatusCode = StatusCode.Error, Message = "down" });

            var cut = Render();

            Assert.Equal("League 7", Cells(cut)[3]);
            Assert.Equal("Song 88 (S88)", Cells(cut)[1]);
        }

        [Theory]
        [InlineData("Create", "Assigned")]
        [InlineData("Update", "State changed")]
        [InlineData("Delete", "Unassigned")]
        public void TheChange_IsNamedAfterTheActivity(string activity, string expected)
        {
            ServeLogs(new List<ActivityLogModel> { Row("S88.1.10.7", activity) });

            var cut = Render();

            Assert.Equal(expected, Cells(cut)[4]);
        }

        // The payload and the user name are text from the database. They are written as text, never as markup.
        [Fact]
        public void TextFromTheLog_IsShownAsText_NotAsMarkup()
        {
            ServeLogs(new List<ActivityLogModel>
            {
                Row("S88.1.10.7", from: "<b id='from'>x</b>", user: "<img src=x onerror=alert(1)>"),
                Row("<script id='bad'>alert(1)</script>")
            });

            var cut = Render();

            Assert.Empty(cut.FindAll("img"));
            Assert.Empty(cut.FindAll("#from"));
            Assert.Empty(cut.FindAll("#bad"));
            Assert.Equal("<img src=x onerror=alert(1)>", Cells(cut)[7]);
            Assert.Equal("<b id='from'>x</b>", Cells(cut)[5]);
            // a payload that is not in the expected shape is shown as it is, never dropped
            Assert.Equal("<script id='bad'>alert(1)</script>", Cells(cut, 1)[3]);
        }

        [Fact]
        public void WithNoRows_ItSaysNoRecordFound()
        {
            var cut = Render();

            Assert.Contains("No Record Found", cut.Markup);
            Assert.DoesNotContain("could not be loaded", cut.Markup);
        }

        // A failed read must not look like an empty history (the same trap as the other View Logs dialogs, BO-113).
        [Fact]
        public void WhenTheReadFails_ItShowsAnError_NotNoRecordFound()
        {
            _backofficeService
                .Setup(s => s.GetActivityLogs(It.IsAny<ActivityLogRequest>()))
                .ReturnsAsync(new BaseResponse<ActivityLogResponse> { StatusCode = StatusCode.Error, Message = "procedure missing" });

            var parameters = new ModalParameters();
            var cut = Render<ViewLeagueLogs>(ps => ps.AddCascadingValue(parameters));
            cut.WaitForAssertion(() => Assert.Contains("could not be loaded", cut.Markup));

            Assert.DoesNotContain("No Record Found", cut.Markup);
            Assert.DoesNotContain("procedure missing", cut.Markup);
        }

        [Fact]
        public void ChoosingAClientAndASport_SendsThemInThePayload_FromTheFirstPage()
        {
            var cut = Render();

            cut.Find("#league-log-client").Change("S88");
            cut.WaitForAssertion(() => Assert.Equal("S88.%%.%%.%%", _calls.Last().Payload));
            cut.Find("#league-log-sport").Change("2");
            cut.WaitForAssertion(() => Assert.Equal("S88.2.%%.%%", _calls.Last().Payload));

            Assert.All(_calls, c => Assert.Equal("SBC.OE.Config.Leagues", c.Page));
            Assert.Equal(1, _calls.Last().CurrentPage);
        }

        [Fact]
        public void ASearch_SendsTheText_AndTheCodesOfTheClientsItMatches()
        {
            var cut = Render();

            cut.Find("#league-log-search").Change("song");
            cut.Find("form").Submit();

            cut.WaitForAssertion(() => Assert.Equal("song", _calls.Last().SearchText));
            Assert.Equal(new[] { "S88" }, _calls.Last().Terms);
        }

        [Fact]
        public void Close_ClosesTheModal()
        {
            var cut = Render();

            cut.Find("button.modal-close").Click();

            _modalService.Verify(m => m.Cancel(), Times.Once);
        }
    }
}
