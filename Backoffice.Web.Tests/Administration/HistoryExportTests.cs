using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using System.Text.Json;
using Xunit;
using CancelBetHistoryPage = Backoffice.Web.Pages.Administration.CancelBetHistory;
using ManualBetSettlementPage = Backoffice.Web.Pages.Administration.ManualBetSettlement;
using MarketSettlementPage = Backoffice.Web.Pages.Administration.MarketSettlement;
using MessageSettlementPage = Backoffice.Web.Pages.Administration.MessageSettlement;

namespace Backoffice.Web.Tests.Administration
{
    // BO-136: "Export To Excel" on the four history pages used to show "Success! Excel file is being generated"
    // while nothing was sent anywhere. Now the page asks the API for every row for the filters on screen
    // (CurrentPage 0), downloads them as a CSV, and says so when the request fails or finds nothing.
    public class HistoryExportTests : BunitTestContext
    {
        private readonly Mock<IBackofficeService> _backofficeService = new();
        private readonly Mock<ITradingService> _tradingService = new();
        private readonly ToastService _toastService = new();

        public HistoryExportTests()
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
            userState.Model.Name = "Test User";
            userState.Model.Claims.Add(new Claim("Admin.CancelBets", $"{(int)Access.Edit}"));

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
            Services.AddSingleton(_toastService);
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                authenticationService.Object,
                sp.GetRequiredService<NavigationManager>()));

            var authContext = this.AddAuthorization();
            authContext.SetAuthorized("test-user");
            authContext.SetPolicies("admin-cancel-history", "admin-settlement-manual", "admin-settlement-market", "admin-settlement-message");

            // The Manual Bet Settlement filter lists the sports when it opens.
            _backofficeService
                .Setup(s => s.GetSportLookup(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<SportsLookupResponse> { StatusCode = StatusCode.Success, Result = new SportsLookupResponse() });

            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static BaseResponse<ActivityLogResponse> Logs(params ActivityLogModel[] logs) => new()
        {
            StatusCode = StatusCode.Success,
            Message = "Success",
            Result = new ActivityLogResponse
            {
                ActivityLogs = logs.ToList(),
                MetaData = new MetaData { CurrentPage = 1, PageSize = 10, TotalCount = logs.Length, TotalPages = 1 }
            }
        };

        private static BaseResponse<ActivityLogResponse> Failure(string message) => new()
        {
            StatusCode = StatusCode.Error,
            Message = message,
            Result = new ActivityLogResponse { ActivityLogs = new List<ActivityLogModel>() }
        };

        private static bool IsExport(MetaData? metaData) => metaData?.CurrentPage == 0;

        private (string FileName, string Csv) TheDownload()
        {
            var invocation = Assert.Single(JSInterop.Invocations, i => i.Identifier == "downloadFileFromText");
            Assert.Equal("text/csv", invocation.Arguments[2]);
            return ((string)invocation.Arguments[0]!, (string)invocation.Arguments[1]!);
        }

        private void AssertNoDownload() => Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "downloadFileFromText");

        private string TheToast() => Assert.Single(_toastService.GetToasts()).Message;

        private static AngleSharp.Dom.IElement ExportButton<T>(IRenderedComponent<T> cut) where T : IComponent =>
            cut.FindAll("button, a").First(e => e.TextContent.Trim().Equals("Export To Excel", StringComparison.OrdinalIgnoreCase));

        #region Cancel Bet History

        private static ActivityLogModel ACancelLog(Guid betId, string reason) => new()
        {
            Id = 1,
            UserName = "operator1",
            From = JsonSerializer.Serialize(new CancelBetHistoryModel.BetDetails { BetId = betId, UserId = "member01", ClientName = "Client A", Reason = reason }),
            To = JsonSerializer.Serialize(new CancelBetHistoryModel.BetSelectionDetails { SportName = "Soccer", FixtureName = "A vs. B" })
        };

        [Fact]
        public void CancelBetHistory_ExportDownloadsEveryRowForTheFiltersInsteadOfClaimingSuccess()
        {
            var shown = Guid.NewGuid();
            var onAnotherPage = Guid.NewGuid();
            _backofficeService
                .Setup(s => s.GetActivityLogs(It.Is<ActivityLogRequest>(r => !IsExport(r.MetaData))))
                .ReturnsAsync(Logs(ACancelLog(shown, "Duplicate")));
            _backofficeService
                .Setup(s => s.GetActivityLogs(It.Is<ActivityLogRequest>(r => IsExport(r.MetaData))))
                .ReturnsAsync(Logs(ACancelLog(shown, "Duplicate"), ACancelLog(onAnotherPage, "Wrong odds")));

            var cut = Render<CancelBetHistoryPage>();
            cut.WaitForAssertion(() => Assert.Contains(shown.ToString(), cut.Markup));

            ExportButton(cut).Click();

            cut.WaitForAssertion(() => TheDownload());
            var (fileName, csv) = TheDownload();
            Assert.StartsWith("cancel-bet-history-", fileName);
            Assert.EndsWith(".csv", fileName);
            Assert.Contains($"1,", csv);
            Assert.Contains(shown.ToString(), csv);
            Assert.Contains(onAnotherPage.ToString(), csv);
            Assert.Contains("Wrong odds", csv);
            Assert.Empty(_toastService.GetToasts());
        }

        [Fact]
        public void CancelBetHistory_TheTableAndTheExportBothUseTheDateRange()
        {
            var requests = new List<ActivityLogRequest>();
            _backofficeService
                .Setup(s => s.GetActivityLogs(It.IsAny<ActivityLogRequest>()))
                .Callback<ActivityLogRequest>(r => requests.Add(r))
                .ReturnsAsync(Logs(ACancelLog(Guid.NewGuid(), "Duplicate")));

            var cut = Render<CancelBetHistoryPage>();
            cut.WaitForAssertion(() => Assert.Single(requests));

            ExportButton(cut).Click();
            cut.WaitForAssertion(() => Assert.Equal(2, requests.Count));

            Assert.All(requests, r =>
            {
                Assert.NotEqual(DateTime.MinValue, r.DateFrom);
                Assert.NotEqual(DateTime.MinValue, r.DateTo);
            });
            Assert.Equal(requests[0].DateFrom, requests[1].DateFrom);
            Assert.Equal(requests[0].DateTo, requests[1].DateTo);
        }

        [Fact]
        public void CancelBetHistory_AFailedExportSaysSoAndDownloadsNothing()
        {
            _backofficeService
                .Setup(s => s.GetActivityLogs(It.Is<ActivityLogRequest>(r => !IsExport(r.MetaData))))
                .ReturnsAsync(Logs(ACancelLog(Guid.NewGuid(), "Duplicate")));
            _backofficeService
                .Setup(s => s.GetActivityLogs(It.Is<ActivityLogRequest>(r => IsExport(r.MetaData))))
                .ReturnsAsync(Failure("Error (Service Unavailable)"));

            var cut = Render<CancelBetHistoryPage>();
            ExportButton(cut).Click();

            cut.WaitForAssertion(() => Assert.Equal("Export failed: Error (Service Unavailable)", TheToast()));
            AssertNoDownload();
        }

        [Fact]
        public void CancelBetHistory_NothingToExportSaysSo()
        {
            _backofficeService
                .Setup(s => s.GetActivityLogs(It.IsAny<ActivityLogRequest>()))
                .ReturnsAsync(Logs());

            var cut = Render<CancelBetHistoryPage>();
            ExportButton(cut).Click();

            cut.WaitForAssertion(() => Assert.Equal("There are no records to export for these filters.", TheToast()));
            AssertNoDownload();
        }

        #endregion

        #region Manual Bet Settlement

        private static ActivityLogModel AManualSettlementLog(string betId) => new()
        {
            Id = 1,
            UserName = "operator1",
            To = JsonSerializer.Serialize(new ManualBetDetailsModel.ManualBetDetails
            {
                BetId = betId,
                UserId = "member01",
                BetStatusName = "Running",
                NewBetStatusName = "Won",
                Reason = "Feed missing",
                Selections = new List<BetSelectionDetails> { new() { SportName = "Soccer" } }
            })
        };

        [Fact]
        public void ManualBetSettlement_ExportDownloadsEveryRowForTheFilters()
        {
            _backofficeService
                .Setup(s => s.GetManualBetSettlementActivityLogs(It.Is<ManualBetSettlementActivityLogRequest>(r => !IsExport(r.MetaData))))
                .ReturnsAsync(Logs(AManualSettlementLog("bet-shown")));
            _backofficeService
                .Setup(s => s.GetManualBetSettlementActivityLogs(It.Is<ManualBetSettlementActivityLogRequest>(r => IsExport(r.MetaData))))
                .ReturnsAsync(Logs(AManualSettlementLog("bet-shown"), AManualSettlementLog("bet-on-page-2"), new ActivityLogModel { To = "not json" }));

            var cut = Render<ManualBetSettlementPage>();
            cut.WaitForAssertion(() => Assert.Contains("bet-shown", cut.Markup));

            ExportButton(cut).Click();

            cut.WaitForAssertion(() => TheDownload());
            var (fileName, csv) = TheDownload();
            Assert.StartsWith("manual-bet-settlement-history-", fileName);
            Assert.Contains("1,", csv);
            Assert.Contains("bet-shown", csv);
            Assert.Contains("\r\n2,", csv);
            Assert.Contains("bet-on-page-2", csv);
            Assert.DoesNotContain("\r\n3,", csv);
            Assert.Empty(_toastService.GetToasts());
        }

        [Fact]
        public void ManualBetSettlement_AFailedExportSaysSoAndDownloadsNothing()
        {
            _backofficeService
                .Setup(s => s.GetManualBetSettlementActivityLogs(It.Is<ManualBetSettlementActivityLogRequest>(r => !IsExport(r.MetaData))))
                .ReturnsAsync(Logs(AManualSettlementLog("bet-shown")));
            _backofficeService
                .Setup(s => s.GetManualBetSettlementActivityLogs(It.Is<ManualBetSettlementActivityLogRequest>(r => IsExport(r.MetaData))))
                .ReturnsAsync(Failure("Error (timeout)"));

            var cut = Render<ManualBetSettlementPage>();
            cut.WaitForAssertion(() => Assert.Contains("bet-shown", cut.Markup));

            ExportButton(cut).Click();

            cut.WaitForAssertion(() => Assert.Equal("Export failed: Error (timeout)", TheToast()));
            AssertNoDownload();
        }

        #endregion

        #region Market Settlement

        private static ActivityLogModel AMarketSettlementLog(string fixture, int sportId) => new()
        {
            Id = 1,
            Payload = JsonSerializer.Serialize(new ManualMarketSettlementAuditModel
            {
                SportId = sportId,
                FixtureName = fixture,
                MarketName = "1x2",
                SportName = "Soccer",
                ManualSettledBy = "operator1",
                ToStatusName = "Won"
            })
        };

        [Fact]
        public void MarketSettlement_ExportDownloadsEveryRowThatMatchesTheFilters()
        {
            _backofficeService
                .Setup(s => s.GetMarketSettlementActivityLogs(It.IsAny<ActivityLogRequest>()))
                .ReturnsAsync(Logs(AMarketSettlementLog("Arsenal vs. Chelsea", 1), AMarketSettlementLog("Lakers vs. Celtics", 2)));

            var cut = Render<MarketSettlementPage>();
            cut.WaitForAssertion(() => Assert.Contains("Lakers", cut.Markup));

            ExportButton(cut).Click();

            cut.WaitForAssertion(() => TheDownload());
            var (fileName, csv) = TheDownload();
            Assert.StartsWith("market-settlement-history-", fileName);
            Assert.Contains("Arsenal vs. Chelsea", csv);
            Assert.Contains("Lakers vs. Celtics", csv);
            _backofficeService.Verify(s => s.GetMarketSettlementActivityLogs(It.Is<ActivityLogRequest>(r => IsExport(r.MetaData))), Times.Once);
            Assert.Empty(_toastService.GetToasts());
        }

        [Fact]
        public void MarketSettlement_AFailedExportSaysSoAndDownloadsNothing()
        {
            _backofficeService
                .Setup(s => s.GetMarketSettlementActivityLogs(It.Is<ActivityLogRequest>(r => !IsExport(r.MetaData))))
                .ReturnsAsync(Logs(AMarketSettlementLog("Arsenal vs. Chelsea", 1)));
            _backofficeService
                .Setup(s => s.GetMarketSettlementActivityLogs(It.Is<ActivityLogRequest>(r => IsExport(r.MetaData))))
                .ReturnsAsync(Failure("Error (Forbidden)"));

            var cut = Render<MarketSettlementPage>();
            cut.WaitForAssertion(() => Assert.Contains("Arsenal", cut.Markup));

            ExportButton(cut).Click();

            cut.WaitForAssertion(() => Assert.Equal("Export failed: Error (Forbidden)", TheToast()));
            AssertNoDownload();
        }

        #endregion

        #region Message Settlement

        private static ActivityLogModel AMessageSettlementLog(string betId) => new()
        {
            Id = 1,
            UserName = "operator2",
            To = JsonSerializer.Serialize(new MessageSettlementTo { BetId = betId, SportName = "Tennis", FixtureName = "A vs. B", ProductId = 1, Reason = "Resent" })
        };

        [Fact]
        public void MessageSettlement_ExportDownloadsEveryRowForTheFilters()
        {
            _backofficeService
                .Setup(s => s.GetActivityLogs(It.IsAny<ActivityLogRequest>()))
                .ReturnsAsync(Logs(AMessageSettlementLog("bet-1"), AMessageSettlementLog("bet-2")));

            var cut = Render<MessageSettlementPage>();

            ExportButton(cut).Click();

            cut.WaitForAssertion(() => TheDownload());
            var (fileName, csv) = TheDownload();
            Assert.StartsWith("message-settlement-history-", fileName);
            Assert.Contains("bet-1", csv);
            Assert.Contains("bet-2", csv);
            _backofficeService.Verify(s => s.GetActivityLogs(It.Is<ActivityLogRequest>(r => IsExport(r.MetaData) && r.Page == "Admin.Settlements.MessageSettlement")), Times.Once);
            Assert.Empty(_toastService.GetToasts());
        }

        [Fact]
        public void MessageSettlement_AFailedExportSaysSoAndDownloadsNothing()
        {
            _backofficeService
                .Setup(s => s.GetActivityLogs(It.IsAny<ActivityLogRequest>()))
                .ReturnsAsync(Failure("Error (Bad Gateway)"));

            var cut = Render<MessageSettlementPage>();

            ExportButton(cut).Click();

            cut.WaitForAssertion(() => Assert.Equal("Export failed: Error (Bad Gateway)", TheToast()));
            AssertNoDownload();
        }

        #endregion
    }
}
