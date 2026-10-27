using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;
using SharedPriceBoostLogModel = Backoffice.Shared.Models.PriceBoostLogModel;
using WebClient = Backoffice.Web.Models.Client;

namespace Backoffice.Web.Tests.Common
{
    // The View Logs modal of Manage Boost and Boosted Settings (BO-42). The BO API returns only the log rows whose
    // client code is in the UserClients the modal sends, so which clients the modal sends decides which rows exist
    // on screen. On DEV, OG Sports (OGE) is a platform client, listed on both pages and valid for a boost, but not
    // one of the user's sportsbook clients, and the rows it saved never appeared. The BO API is a mock here, so
    // these cover what the modal asks for and what it keeps.
    public class ViewAllLogsModalTests : BunitTestContext
    {
        private readonly UserState _userState = new();
        private readonly Mock<IBackofficeService> _backofficeService = new();
        private readonly List<PriceboostActivityLogRequest> _logRequests = new();
        private readonly List<ActivityLogModel> _storedLogs = new();

        public ViewAllLogsModalTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());

            SignIn("MRD,OGE,OKT");

            _backofficeService.Setup(s => s.GetSportLookup(It.IsAny<string>()))
                .ReturnsAsync(Reply(new SportsLookupResponse { SportLookup = new List<SportLookupModel>() }));

            _backofficeService
                .Setup(s => s.GetPriceboostActivityLogs(It.IsAny<PriceboostActivityLogRequest>(), It.IsAny<string>()))
                .Callback<PriceboostActivityLogRequest, string>((r, _) => _logRequests.Add(r))
                .ReturnsAsync(() => Reply(new ActivityLogResponse
                {
                    ActivityLogs = _storedLogs.ToList(),
                    MetaData = new MetaData { CurrentPage = 1, PageSize = 20, TotalCount = _storedLogs.Count, TotalPages = 1 }
                }));

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
            Services.AddSingleton(_userState);
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                new Mock<IAuthenticationService>().Object,
                sp.GetRequiredService<NavigationManager>()));
        }

        // Signs a user in the way the app does, from a token whose CLIENTCODES claim lists the clients the user may
        // see: UserState keeps only those clients in both of its lists (Super Admin aside). The sportsbook list is
        // then the narrower one the platform's applications allow, here without OG Sports.
        private void SignIn(string clientCodes)
        {
            var payload = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["UserId"] = "test-user",
                ["TokenName"] = "Test User",
                ["ClientCodes"] = clientCodes
            });
            _userState.SetAccessToken("header." + Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)).TrimEnd('=') + ".signature");

            _userState.SetSportsbookClients(new List<WebClient>
            {
                new WebClient { ClientId = 11, ClientCode = "MRD", ClientName = "Meridian Bet" },
                new WebClient { ClientId = 12, ClientCode = "OKT", ClientName = "Oakton Sports" }
            });
            _userState.SetTradingClients(new List<PlatformClientResponse>
            {
                new PlatformClientResponse { ClientId = Guid.NewGuid(), ClientCode = "MRD", ClientName = "Meridian Bet" },
                new PlatformClientResponse { ClientId = Guid.NewGuid(), ClientCode = "OGE", ClientName = "OG Sports" },
                new PlatformClientResponse { ClientId = Guid.NewGuid(), ClientCode = "OKT", ClientName = "Oakton Sports" }
            });
        }

        private static BaseResponse<T> Reply<T>(T result) =>
            new BaseResponse<T> { StatusCode = StatusCode.Success, Message = "Success", Result = result };

        private IRenderedComponent<ViewAllLogsModal> Open(SportsbookClientPermission page)
        {
            var parameters = new ModalParameters();
            parameters.Add("Region", "hk");
            parameters.Add("SelectedPage", page);

            return Render<ViewAllLogsModal>(ps => ps.AddCascadingValue(parameters));
        }

        // A Manage Boost row as the Add Boosted Odds dialog writes it: Payload is client.sport.segment.league.
        private static ActivityLogModel BoostLog(string clientCode, string clientName) => new ActivityLogModel
        {
            Page = "SBC.OE.PriceBoost.Manage",
            Payload = $"{clientCode}.10.223.145",
            Activity = "Create",
            From = "-",
            To = JsonSerializer.Serialize(new SharedPriceBoostLogModel
            {
                ClientCode = clientCode,
                ClientName = clientName,
                SportName = "Football",
                SelectionName = "Asteras Tripolis",
                MarketName = "Away Team No Bet",
                SegmentName = "Greece",
                LeagueName = "Greece Cup",
                FixtureName = "Asteras Tripolis vs. Kifisia FC",
                Odds = 1.60m
            }),
            UserId = "test-user",
            UserName = "superadmin"
        };

        // A Boosted Settings row: Payload is the client code alone.
        private static ActivityLogModel SettingsLog(string clientCode, decimal cap) => new ActivityLogModel
        {
            Page = "SBC.OE.PriceBoost.Settings",
            Payload = clientCode,
            Activity = "Create",
            From = "-",
            To = JsonSerializer.Serialize(new BoostedSettingsModel { ClientCode = clientCode, Cap = cap }),
            UserId = "test-user",
            UserName = "superadmin"
        };

        [Fact]
        public void ManageBoost_AsksForTheLogsOfEveryClientThePageLists()
        {
            var cut = Open(SportsbookClientPermission.SBC_OE_PriceBoost_Manage);

            cut.WaitForAssertion(() =>
            {
                var request = Assert.Single(_logRequests.GroupBy(r => string.Join(",", r.UserClients)).Select(g => g.First()));
                Assert.Equal(new[] { "MRD", "OGE", "OKT" }, request.UserClients);
                Assert.Equal("SBC.OE.PriceBoost.Manage", request.Page);
            });
        }

        [Fact]
        public void BoostedSettings_AsksForTheLogsOfEveryClientThePageLists()
        {
            var cut = Open(SportsbookClientPermission.SBC_OE_PriceBoost_Settings);

            cut.WaitForAssertion(() =>
            {
                var request = Assert.Single(_logRequests.GroupBy(r => string.Join(",", r.UserClients)).Select(g => g.First()));
                Assert.Equal(new[] { "MRD", "OGE", "OKT" }, request.UserClients);
                Assert.Equal("SBC.OE.PriceBoost.Settings", request.Page);
            });
        }

        [Fact]
        public void AsksOnlyForTheClientsThePermissionsAllow()
        {
            // the fix must not widen anything: a client outside the user's CLIENTCODES is still not asked for
            SignIn("MRD,OGE");

            var cut = Open(SportsbookClientPermission.SBC_OE_PriceBoost_Manage);

            cut.WaitForAssertion(() =>
            {
                var request = Assert.Single(_logRequests.GroupBy(r => string.Join(",", r.UserClients)).Select(g => g.First()));
                Assert.Equal(new[] { "MRD", "OGE" }, request.UserClients);
            });
        }

        [Fact]
        public void ManageBoost_ShowsTheRowsOfAllThreeClients()
        {
            _storedLogs.AddRange(new[]
            {
                BoostLog("MRD", "Meridian Bet"), BoostLog("OGE", "OG Sports"), BoostLog("OKT", "Oakton Sports")
            });

            var cut = Open(SportsbookClientPermission.SBC_OE_PriceBoost_Manage);

            cut.WaitForAssertion(() =>
            {
                Assert.Contains("Meridian Bet", cut.Markup);
                Assert.Contains("OG Sports", cut.Markup);
                Assert.Contains("Oakton Sports", cut.Markup);
            });
        }

        [Fact]
        public void BoostedSettings_ShowsTheRowOfAClientThatIsNotASportsbookClient()
        {
            _storedLogs.AddRange(new[] { SettingsLog("MRD", 0.05m), SettingsLog("OGE", 0.07m) });

            var cut = Open(SportsbookClientPermission.SBC_OE_PriceBoost_Settings);

            cut.WaitForAssertion(() =>
            {
                Assert.Contains("Meridian Bet", cut.Markup);
                Assert.Contains("OG Sports", cut.Markup);
                Assert.Contains("7.00 %", cut.Markup);
            });
        }
    }
}
