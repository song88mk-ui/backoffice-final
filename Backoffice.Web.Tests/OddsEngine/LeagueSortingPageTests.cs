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
using System.Text.RegularExpressions;
using Xunit;
using LeagueSortingPage = Backoffice.Web.Pages.OddsEngine.LeagueSorting;

namespace Backoffice.Web.Tests.OddsEngine
{
    // Page-level tests for the League Sorting export and import actions (BO-122): who gets which icon, what the
    // export downloads, and that the import icon opens the dialog.
    public class LeagueSortingPageTests : BunitTestContext
    {
        private const string PermissionClaim = "SBC.OE.LeagueSorting";

        private readonly Mock<IBackofficeService> _backofficeService = new();
        private readonly UserState _userState = new();

        public LeagueSortingPageTests()
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

            _userState.Model.Id = "test-user";
            _userState.Model.Name = "Test User";
            _userState.Model.TradingClients.Add(new PlatformClientResponse
            {
                ClientId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                ClientCode = "QTE-A",
                ClientName = "Client A"
            });

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
            Services.AddSingleton(_userState);
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                authenticationService.Object,
                sp.GetRequiredService<NavigationManager>()));

            var authContext = this.AddAuthorization();
            authContext.SetAuthorized("test-user");
            authContext.SetPolicies("sbc-league-sorting", "sbc-league-sorting-canedit");

            JSInterop.Mode = JSRuntimeMode.Loose;

            _backofficeService
                .Setup(s => s.GetSportLookup(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<SportsLookupResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new SportsLookupResponse
                    {
                        SportLookup = new List<SportLookupModel> { new SportLookupModel { SportId = 1, SportName = "Football" } }
                    }
                });

            _backofficeService
                .Setup(s => s.GetClientOddstableLeagueOrderList(It.IsAny<Backoffice.Shared.Models.Requests.ClientOddstableLeagueOrderListRequest>()))
                .ReturnsAsync(new BaseResponse<List<ClientOddstableLeagueOrderListResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<ClientOddstableLeagueOrderListResponse>
                    {
                        new ClientOddstableLeagueOrderListResponse { ClientCode = "QTE-A", ClientName = "Client A", SportId = 1, SegmentId = 20, SegmentName = "Spain", LeagueId = 200, LeagueName = "La Liga", SortOrder = 2 },
                        new ClientOddstableLeagueOrderListResponse { ClientCode = "QTE-A", ClientName = "Client A", SportId = 1, SegmentId = 10, SegmentName = "England", LeagueId = 100, LeagueName = "Premier League", SortOrder = 1 }
                    }
                });
        }

        private IRenderedComponent<LeagueSortingPage> RenderPage(Access access)
        {
            _userState.Model.Claims.Add(new Claim(PermissionClaim, $"{(int)access}"));

            var cut = Render<LeagueSortingPage>();
            cut.WaitForAssertion(() => Assert.Contains("Premier League", cut.Markup));

            return cut;
        }

        [Fact]
        public void AnEditor_HasBothTheExportAndTheImportIcon()
        {
            var cut = RenderPage(Access.Edit);

            Assert.Single(cut.FindAll("a[title='Export CSV']"));
            Assert.Single(cut.FindAll("a[title='Import CSV']"));
        }

        [Fact]
        public void AViewer_CanExport_ButTheImportIconIsDisabled()
        {
            var cut = RenderPage(Access.View);

            Assert.Single(cut.FindAll("a[title='Export CSV']"));
            Assert.Empty(cut.FindAll("a[title='Import CSV']"));
            Assert.NotEmpty(cut.FindAll("a.ls-link-disabled .fa-upload"));
        }

        [Fact]
        public void Export_DownloadsTheClientsLeaguesInSortOrder_AsACsvNamedAfterTheClientAndSport()
        {
            var cut = RenderPage(Access.View);

            cut.Find("a[title='Export CSV']").Click();

            var download = Assert.Single(JSInterop.Invocations["downloadFileFromText"]);
            var fileName = (string)download.Arguments[0]!;
            var text = (string)download.Arguments[1]!;

            Assert.Matches(new Regex(@"^LeagueSorting-QTE-A-Football-\d{8}-\d{6}\.csv$"), fileName);
            Assert.Equal("text/csv", download.Arguments[2]);
            Assert.Equal(new[]
            {
                "ClientCode,SportId,SegmentId,SegmentName,LeagueId,LeagueName,SortOrder",
                "QTE-A,1,10,England,100,Premier League,1",
                "QTE-A,1,20,Spain,200,La Liga,2"
            }, text.TrimStart('﻿').Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries));
        }

        [Fact]
        public void Import_OpensTheDialogForThatClientAndSport()
        {
            var cut = RenderPage(Access.Edit);

            cut.Find("a[title='Import CSV']").Click();

            cut.WaitForAssertion(() => Assert.Contains("Import League Sorting of", cut.Markup));
            Assert.Contains("CSV file", cut.Markup);
            Assert.Contains("Client A", cut.Markup);
        }
    }
}
