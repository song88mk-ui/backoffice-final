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
using Xunit;
using ResendSettlementPage = Backoffice.Web.Pages.Administration.ResendSettlement;

namespace Backoffice.Web.Tests.Administration
{
    // BO-125: Resend Settlement refuses Cancelled bets and replays to the operator, so a bet that is Cancelled
    // here but still open at Bet Genius needs its own action. "Resend Cancellation to Bet Genius" is offered
    // only when every listed bet is loaded and Cancelled, asks for a reason like the settlement action does,
    // confirms in the same dialog, and sends the bet ids to the provider-cancellation endpoint. These tests
    // drive the page the way the operator does: type a bet id, read what the page offers, click, confirm.
    public class ResendSettlementPageTests : BunitTestContext
    {
        private const string ProviderButton = "button.btn-resend-provider-cancellation";
        private const string ProviderConfirmation = "Please confirm to send the cancellation of all reference IDs added to Bet Genius.";
        private const string SettlementConfirmation = "Please confirm to settle all reference IDs added.";

        private readonly Mock<IBackofficeService> _backofficeService = new();

        public ResendSettlementPageTests()
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
            userState.Model.Claims.Add(new Claim("Admin.Settlements.ResendSettlement", $"{(int)Access.Edit}"));

            Services.AddSingleton<IConfiguration>(configuration);
            // CustomModal (rendered by the page) casts IModalService to the concrete ModalService.
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

            var authContext = this.AddAuthorization();
            authContext.SetAuthorized("test-user");
            authContext.SetPolicies("admin-settlement-resend");

            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static ResendSettlementBetDetailModel ABet(Guid betId, int statusId, string statusName, string reason = "103 - Operator Error") =>
            new ResendSettlementBetDetailModel
            {
                Bet = new ResendSettlementBetDetailResponse.ResendSettlementBetDetail
                {
                    BetId = betId,
                    BetStatusId = statusId,
                    BetStatusName = statusName,
                    Reason = reason,
                    BetDate = new DateTime(2026, 10, 14, 18, 0, 0),
                    BetSelections = new List<ResendSettlementBetDetailResponse.ResendSettlementBetSelectionDetail>
                    {
                        new ResendSettlementBetDetailResponse.ResendSettlementBetSelectionDetail
                        {
                            BetSelectionId = Guid.NewGuid(),
                            BetId = betId,
                            LeagueName = "UEFA Champions League",
                            FixtureName = "Paris Saint-Germain vs Como",
                            SelectionName = "Draw",
                            FixtureStartDate = new DateTime(2026, 10, 14, 19, 0, 0)
                        }
                    }
                }
            };

        private static ResendSettlementBetDetailModel ACancelledBet(Guid? betId = null, string reason = "103 - Operator Error") =>
            ABet(betId ?? Guid.NewGuid(), (int)BetStatus.Cancelled, "Cancelled", reason);

        // Types each bet id into its own row, the way the operator does, adding a row for every bet after the first.
        private IRenderedComponent<ResendSettlementPage> RenderWithBets(params ResendSettlementBetDetailModel[] bets)
        {
            foreach (var bet in bets)
            {
                _backofficeService
                    .Setup(s => s.GetResendSettlementBetDetails(It.Is<ResendSettlementBetDetailRequest>(r => r.BetId == bet.Bet.BetId.ToString())))
                    .ReturnsAsync(new BaseResponse<ResendSettlementBetDetailModel> { StatusCode = StatusCode.Success, Result = bet });
            }

            var cut = Render<ResendSettlementPage>();

            for (var i = 0; i < bets.Length; i++)
            {
                if (i > 0)
                    cut.Find("button.link-add-row").Click();

                cut.FindAll("tbody tr input")[i].Input(bets[i].Bet.BetId.ToString()!);
                var expectedStatus = bets[i].Bet.BetStatusName;
                var row = i;
                cut.WaitForAssertion(() => Assert.Contains(expectedStatus, cut.FindAll("tbody tr")[row].TextContent));
            }

            return cut;
        }

        private static bool IsDisabled(IRenderedComponent<ResendSettlementPage> cut) => cut.Find(ProviderButton).HasAttribute("disabled");

        private static AngleSharp.Dom.IElement SettlementButton(IRenderedComponent<ResendSettlementPage> cut) =>
            cut.FindAll("button").First(b => b.TextContent.Trim() == "Resend Settlement");

        private void SetupProviderCancellation(BaseResponse<ResendSettlementResponse> response) =>
            _backofficeService
                .Setup(s => s.ResendProviderCancellation(It.IsAny<ResendProviderCancellationRequest>()))
                .ReturnsAsync(response);

        private static BaseResponse<ResendSettlementResponse> Answer(StatusCode statusCode, params (Guid BetId, StatusCode StatusCode, string Message)[] bets) =>
            new BaseResponse<ResendSettlementResponse>
            {
                StatusCode = statusCode,
                Message = statusCode.ToString().Replace('_', ' '),
                Result = new ResendSettlementResponse
                {
                    Data = bets.Select(b => new ResendSettlementResponse.ResendSettlementResponseData { BetId = b.BetId, StatusCode = b.StatusCode, Message = b.Message }).ToList()
                }
            };

        [Fact]
        public void BeforeAnyBetIsLoaded_TheProviderButtonIsOfferedButDisabled()
        {
            var cut = Render<ResendSettlementPage>();

            Assert.Equal("Resend Cancellation to Bet Genius", cut.Find(ProviderButton).TextContent.Trim());
            Assert.True(IsDisabled(cut));
        }

        [Fact]
        public void ACancelledBet_EnablesTheProviderButton()
        {
            var cut = RenderWithBets(ACancelledBet());

            Assert.False(IsDisabled(cut));
        }

        [Theory]
        [InlineData(2000, "Running")]
        [InlineData(3000, "Won")]
        [InlineData(4000, "Lost")]
        [InlineData(5000, "Void")]
        [InlineData(6000, "Rejected")]
        [InlineData(8000, "CashedOut")]
        public void ABetThatIsNotCancelled_KeepsTheProviderButtonDisabled(int statusId, string statusName)
        {
            var cut = RenderWithBets(ABet(Guid.NewGuid(), statusId, statusName));

            Assert.True(IsDisabled(cut));
        }

        [Fact]
        public void WhenOneOfTheBetsIsNotCancelled_TheProviderButtonStaysDisabled()
        {
            var cut = RenderWithBets(ACancelledBet(), ABet(Guid.NewGuid(), (int)BetStatus.Won, "Won"));

            Assert.True(IsDisabled(cut));
        }

        [Fact]
        public void WhenEveryBetIsCancelled_TheProviderButtonIsEnabled()
        {
            var cut = RenderWithBets(ACancelledBet(), ACancelledBet());

            Assert.False(IsDisabled(cut));
        }

        [Fact]
        public void ABetThatCouldNotBeFound_KeepsTheProviderButtonDisabled()
        {
            var unknownId = Guid.NewGuid();
            _backofficeService
                .Setup(s => s.GetResendSettlementBetDetails(It.IsAny<ResendSettlementBetDetailRequest>()))
                .ReturnsAsync(new BaseResponse<ResendSettlementBetDetailModel> { StatusCode = StatusCode.Success, Result = new ResendSettlementBetDetailModel() });

            var cut = Render<ResendSettlementPage>();
            cut.FindAll("tbody tr input")[0].Input(unknownId.ToString());

            Assert.True(IsDisabled(cut));
        }

        [Fact]
        public void WithoutAReason_NothingIsConfirmedOrSent()
        {
            var cut = RenderWithBets(ACancelledBet(reason: ""));

            cut.Find(ProviderButton).Click();

            Assert.Contains("Reason is required", cut.Markup);
            Assert.DoesNotContain(ProviderConfirmation, cut.Markup);
            _backofficeService.Verify(s => s.ResendProviderCancellation(It.IsAny<ResendProviderCancellationRequest>()), Times.Never);
        }

        [Fact]
        public void Clicking_AsksForConfirmation_BeforeAnythingIsSent()
        {
            var cut = RenderWithBets(ACancelledBet());

            cut.Find(ProviderButton).Click();

            cut.WaitForAssertion(() => Assert.Contains(ProviderConfirmation, cut.Markup));
            Assert.DoesNotContain(SettlementConfirmation, cut.Markup);
            _backofficeService.Verify(s => s.ResendProviderCancellation(It.IsAny<ResendProviderCancellationRequest>()), Times.Never);
        }

        [Fact]
        public void Confirming_SendsOnlyTheBetIds_ToTheProviderCancellationEndpoint_AndReportsItQueued()
        {
            var firstId = Guid.NewGuid();
            var secondId = Guid.NewGuid();
            SetupProviderCancellation(Answer(StatusCode.Success, (firstId, StatusCode.Success, "Success"), (secondId, StatusCode.Success, "Success")));
            var cut = RenderWithBets(ACancelledBet(firstId), ACancelledBet(secondId));

            cut.Find(ProviderButton).Click();
            cut.WaitForAssertion(() => Assert.Contains(ProviderConfirmation, cut.Markup));
            cut.Find("button.modal-confirm").Click();

            cut.WaitForAssertion(() => Assert.Contains("Cancellation of 2 bet(s) queued for sending to Bet Genius", cut.Markup));
            _backofficeService.Verify(s => s.ResendProviderCancellation(It.Is<ResendProviderCancellationRequest>(r =>
                r.BetIds.SequenceEqual(new[] { firstId.ToString(), secondId.ToString() }))), Times.Once);
            _backofficeService.Verify(s => s.ResendSettlement(It.IsAny<ResendSettlementRequest>()), Times.Never);
        }

        [Fact]
        public void WhenSomeBetsFail_EachFailureIsShownWithItsBetId()
        {
            var okId = Guid.NewGuid();
            var failedId = Guid.NewGuid();
            SetupProviderCancellation(Answer(StatusCode.Error, (okId, StatusCode.Success, "Success"), (failedId, StatusCode.Error, "Bet not found")));
            var cut = RenderWithBets(ACancelledBet(okId), ACancelledBet(failedId));

            cut.Find(ProviderButton).Click();
            cut.WaitForAssertion(() => Assert.Contains(ProviderConfirmation, cut.Markup));
            cut.Find("button.modal-confirm").Click();

            cut.WaitForAssertion(() => Assert.Contains($"1 of 2 queued for Bet Genius. Failed: {failedId.ToString().ToUpper()}: Bet not found", cut.Markup));
        }

        [Fact]
        public void WhenEveryBetFails_OnlyTheFailuresAreShown()
        {
            var failedId = Guid.NewGuid();
            SetupProviderCancellation(Answer(StatusCode.Error, (failedId, StatusCode.Error, "Bet is not cancelled (current status: Won)")));
            var cut = RenderWithBets(ACancelledBet(failedId));

            cut.Find(ProviderButton).Click();
            cut.WaitForAssertion(() => Assert.Contains(ProviderConfirmation, cut.Markup));
            cut.Find("button.modal-confirm").Click();

            cut.WaitForAssertion(() => Assert.Contains($"{failedId.ToString().ToUpper()}: Bet is not cancelled (current status: Won)", cut.Markup));
            Assert.DoesNotContain("queued", cut.Markup);
        }

        [Fact]
        public void WhenTheCallFailsWithoutPerBetResults_TheErrorMessageIsShown()
        {
            SetupProviderCancellation(new BaseResponse<ResendSettlementResponse> { StatusCode = StatusCode.Error, Message = "Error (Bad Gateway)" });
            var cut = RenderWithBets(ACancelledBet());

            cut.Find(ProviderButton).Click();
            cut.WaitForAssertion(() => Assert.Contains(ProviderConfirmation, cut.Markup));
            cut.Find("button.modal-confirm").Click();

            cut.WaitForAssertion(() => Assert.Contains("Error (Bad Gateway)", cut.Markup));
        }

        [Fact]
        public void ResendSettlement_StillRefusesACancelledBet_AndSendsNothing()
        {
            var cut = RenderWithBets(ACancelledBet());

            SettlementButton(cut).Click();

            cut.WaitForAssertion(() => Assert.Contains("Bets under Cancelled, Rejected, Waiting, and Cashout status is not allowed", cut.Markup));
            _backofficeService.Verify(s => s.ResendSettlement(It.IsAny<ResendSettlementRequest>()), Times.Never);
            _backofficeService.Verify(s => s.ResendProviderCancellation(It.IsAny<ResendProviderCancellationRequest>()), Times.Never);
        }

        [Fact]
        public void ResendSettlement_StillConfirmsAndSendsToTheOperator_ForASettledBet()
        {
            var wonId = Guid.NewGuid();
            _backofficeService
                .Setup(s => s.ResendSettlement(It.IsAny<ResendSettlementRequest>()))
                .ReturnsAsync(Answer(StatusCode.Success, (wonId, StatusCode.Success, "Success")));
            var cut = RenderWithBets(ABet(wonId, (int)BetStatus.Won, "Won"));

            SettlementButton(cut).Click();
            cut.WaitForAssertion(() => Assert.Contains(SettlementConfirmation, cut.Markup));
            Assert.DoesNotContain(ProviderConfirmation, cut.Markup);
            cut.Find("button.modal-confirm").Click();

            cut.WaitForAssertion(() => Assert.Contains("Resend to operator of bet settlement, cashout, and/or cancellation successful", cut.Markup));
            _backofficeService.Verify(s => s.ResendSettlement(It.Is<ResendSettlementRequest>(r =>
                r.Bets.Count == 1 && r.Bets[0].BetId == wonId.ToString() && r.Bets[0].CurrentStatus == BetStatus.Won)), Times.Once);
            _backofficeService.Verify(s => s.ResendProviderCancellation(It.IsAny<ResendProviderCancellationRequest>()), Times.Never);
        }
    }
}
