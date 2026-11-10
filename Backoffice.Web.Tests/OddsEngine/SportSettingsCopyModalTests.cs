using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.OddsEngine.SportSettings;
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
    // Component tests for the Sport Settings Copy dialog (BO-160): pick several target clients, confirm, and report
    // which clients were copied. The server is a mock; the per-target copy itself is covered by
    // ClientSportSettingsCopyRepositoryTests in the API tests.
    public class SportSettingsCopyModalTests : BunitTestContext
    {
        private static readonly Guid ClientAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid ClientBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid ClientCId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        private readonly Mock<IBackofficeService> _backofficeService = new();
        private readonly Mock<IModalService> _modalService = new();
        private readonly List<CopyClientSportSettingsRequest> _requests = new();

        public SportSettingsCopyModalTests()
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
            userState.Model.TradingClients = new List<PlatformClientResponse>
            {
                new PlatformClientResponse
                {
                    ClientId = ClientAId,
                    ClientCode = "QTE-A",
                    ClientName = "Client A"
                },
                new PlatformClientResponse
                {
                    ClientId = ClientBId,
                    ClientCode = "QTE-B",
                    ClientName = "Client B"
                },
                new PlatformClientResponse
                {
                    ClientId = ClientCId,
                    ClientCode = "QTE-C",
                    ClientName = "Client C"
                },
            };

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
            Services.AddSingleton(userState);
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                new Mock<IAuthenticationService>().Object,
                sp.GetRequiredService<NavigationManager>()));
        }

        private static List<SportsSortOrder> OnScreenSports() => new List<SportsSortOrder>
        {
            new SportsSortOrder
            {
                SportId = 2,
                SportName = "Basketball",
                SortOrder = 1,
                IsActive = true,
                SegmentsSortOrder = new List<SportsSegmentSortOrder>()
            },
            new SportsSortOrder
            {
                SportId = 1,
                SportName = "Football",
                SortOrder = 2,
                IsActive = true,
                SegmentsSortOrder = new List<SportsSegmentSortOrder>()
            }
        };

        // The server copies every target, except those listed in failing (with that message).
        private void ServerReplies(Dictionary<string, string>? failing = null, string? warning = null)
        {
            failing ??= new Dictionary<string, string>();

            _backofficeService
                .Setup(s => s.CopyClientSportSettings(It.IsAny<CopyClientSportSettingsRequest>()))
                .Callback<CopyClientSportSettingsRequest>(r => _requests.Add(r))
                .ReturnsAsync((CopyClientSportSettingsRequest r) =>
                {
                    var targets = r.ToClientCodes.Select(code => new CopyClientSportSettingsTargetResult
                    {
                        ClientCode = code,
                        StatusCode = failing.ContainsKey(code) ? StatusCode.Error : StatusCode.Success,
                        Message = failing.TryGetValue(code, out var message) ? message : warning ?? "Copied"
                    }).ToList();
                    var allCopied = targets.All(t => t.StatusCode == StatusCode.Success);

                    return new BaseResponse<CopyClientSportSettingsResponse>
                    {
                        StatusCode = allCopied ? StatusCode.Success : StatusCode.Error,
                        Message = allCopied ? "Copy sport settings successful" : "Sport settings copied to some clients",
                        Result = new CopyClientSportSettingsResponse { Targets = targets }
                    };
                });
        }

        // Source on screen: Client A.
        private IRenderedComponent<CopyModal> Render(List<SportsSortOrder>? sports = null)
        {
            var parameters = new ModalParameters();
            parameters.Add("FromClientCode", "QTE-A");
            parameters.Add("Sports", sports ?? OnScreenSports());

            return Render<CopyModal>(ps => ps.AddCascadingValue(parameters));
        }

        private static void Tick(IRenderedComponent<CopyModal> cut, string clientCode) =>
            cut.Find($"li[id='{clientCode}'] input").Click();

        private static List<string> Chips(IRenderedComponent<CopyModal> cut) =>
            cut.FindAll(".ss-chip").Select(c => c.TextContent.Trim()).ToList();

        private static AngleSharp.Dom.IElement CopyButton(IRenderedComponent<CopyModal> cut) =>
            cut.FindAll(".modal-footer button").Single(b => b.TextContent.Trim() == "Copy");

        private static void Click(IRenderedComponent<CopyModal> cut, string text) =>
            cut.FindAll(".modal-footer button").Single(b => b.TextContent.Trim() == text).Click();

        private static Task Confirm(IRenderedComponent<CopyModal> cut) =>
            cut.InvokeAsync(() => cut.FindAll(".modal-footer button").Single(b => b.TextContent.Trim() == "Confirm").Click());

        // ---------------------------------------------------------------

        [Fact]
        public void ListsEveryClientExceptTheSource_UnderAllClients()
        {
            var cut = Render();

            var items = cut.FindAll(".dropdown-menu li").Select(li => li.Id).ToList();

            Assert.Equal(new[] { "all", "QTE-B", "QTE-C" }, items);
            Assert.Contains("Client A", cut.Find(".modal-body").TextContent);
        }

        [Fact]
        public void TickingClients_ShowsTags_AndATagCanBeRemoved()
        {
            var cut = Render();

            Tick(cut, "QTE-B");
            Tick(cut, "QTE-C");
            Assert.Equal(new[] { "Client B", "Client C" }, Chips(cut));

            cut.Find(".ss-chip button").Click();
            Assert.Equal(new[] { "Client C" }, Chips(cut));
        }

        [Fact]
        public void AllClients_TicksEveryoneButTheSource()
        {
            var cut = Render();

            Tick(cut, "all");

            Assert.Equal(new[] { "Client B", "Client C" }, Chips(cut));
        }

        [Fact]
        public void Copy_IsDisabledUntilAClientIsTicked()
        {
            var cut = Render();
            Assert.True(CopyButton(cut).HasAttribute("disabled"));

            Tick(cut, "QTE-B");
            Assert.False(CopyButton(cut).HasAttribute("disabled"));

            cut.Find(".ss-chip button").Click();
            Assert.True(CopyButton(cut).HasAttribute("disabled"));
        }

        [Fact]
        public void ASourceWithNoSports_CannotBeCopied()
        {
            var cut = Render(new List<SportsSortOrder>());

            Tick(cut, "QTE-B");

            Assert.True(CopyButton(cut).HasAttribute("disabled"));
            Assert.Contains("has no sports to copy", cut.Find(".alert-danger").TextContent);
        }

        [Fact]
        public void Confirmation_SaysHowManyClientsWillBeReplaced_AndBackReturns()
        {
            var cut = Render();

            Tick(cut, "QTE-B");
            Tick(cut, "QTE-C");
            Click(cut, "Copy");

            var warning = cut.Find(".alert-warning").TextContent;
            Assert.Contains("2 clients", warning);
            Assert.Contains("Client A", warning);

            Click(cut, "Back");
            Assert.Empty(cut.FindAll(".alert-warning"));
            Assert.Equal(new[] { "Client B", "Client C" }, Chips(cut));
        }

        [Fact]
        public void Confirmation_NamesASingleClient()
        {
            var cut = Render();

            Tick(cut, "QTE-C");
            Click(cut, "Copy");

            Assert.Contains("Client C", cut.Find(".alert-warning").TextContent);
        }

        [Fact]
        public async Task Confirm_SendsTheOnScreenListToEveryTarget_AndClosesOnSuccess()
        {
            ServerReplies();
            var sports = OnScreenSports();
            var cut = Render(sports);

            Tick(cut, "QTE-B");
            Tick(cut, "QTE-C");
            Click(cut, "Copy");
            await Confirm(cut);

            var request = Assert.Single(_requests);
            Assert.Equal("QTE-A", request.FromClientCode);
            Assert.Equal(new[] { "QTE-B", "QTE-C" }, request.ToClientCodes);
            Assert.Same(sports, request.SportsSortOrder);
            Assert.Equal("test-user", request.UserId);
            Assert.Equal("Test User", request.UserName);

            cut.WaitForAssertion(() => _modalService.Verify(m => m.Cancel(), Times.Once), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Confirm_WhenOneClientFails_SaysWhichWereCopiedAndWhichWereNot()
        {
            ServerReplies(new Dictionary<string, string> { ["QTE-C"] = "database unavailable" });
            var cut = Render();

            Tick(cut, "QTE-B");
            Tick(cut, "QTE-C");
            Click(cut, "Copy");
            await Confirm(cut);

            var alert = cut.Find(".alert-danger").TextContent;
            Assert.Contains("Copied to Client B", alert);
            Assert.Contains("Not copied to Client C (database unavailable)", alert);
            _modalService.Verify(m => m.Cancel(), Times.Never);
        }

        [Fact]
        public async Task Confirm_WhenTheCacheWasNotRefreshed_KeepsTheDialogOpenWithTheWarning()
        {
            ServerReplies(warning: "Copied, but the sportsbook cache was not refreshed. Save its Sport Settings once to refresh it.");
            var cut = Render();

            Tick(cut, "QTE-B");
            Click(cut, "Copy");
            await Confirm(cut);

            Assert.Contains("cache was not refreshed", cut.Markup);
            _modalService.Verify(m => m.Cancel(), Times.Never);
        }
    }
}
