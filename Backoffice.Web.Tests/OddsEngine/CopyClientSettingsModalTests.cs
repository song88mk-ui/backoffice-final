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
    // Component tests for Config > Copy Client Settings (BO-162): one From client, several To clients, a confirmation
    // step with the count, and which clients were copied. The server is a mock; the copy itself is covered by
    // ClientConfigCopyRepositoryTests in the API tests.
    public class CopyClientSettingsModalTests : BunitTestContext
    {
        private static readonly Guid ClientAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid ClientBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid ClientCId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        private readonly Mock<IBackofficeService> _backofficeService = new();
        private readonly Mock<IModalService> _modalService = new();
        private readonly List<CopyClientConfigRequest> _requests = new();
        private readonly List<AlertModel> _pageAlerts = new();
        private readonly UserState _userState = new();

        public CopyClientSettingsModalTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());

            _userState.Model.Id = "test-user";
            _userState.Model.Name = "Test User";
            _userState.Model.TradingClients = Clients("QTE-A", "QTE-B", "QTE-C");

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
        }

        private static List<PlatformClientResponse> Clients(params string[] codes)
        {
            var ids = new Dictionary<string, Guid> { ["QTE-A"] = ClientAId, ["QTE-B"] = ClientBId, ["QTE-C"] = ClientCId };

            return codes.Select(code => new PlatformClientResponse
            {
                ClientId = ids[code],
                ClientCode = code,
                ClientName = $"Client {code[^1]}"
            }).ToList();
        }

        // The server copies every target, except those listed in failing (with that message).
        private void ServerReplies(Dictionary<string, string>? failing = null)
        {
            failing ??= new Dictionary<string, string>();

            _backofficeService
                .Setup(s => s.CopyClientConfig(It.IsAny<CopyClientConfigRequest>()))
                .Callback<CopyClientConfigRequest>(r => _requests.Add(r))
                .ReturnsAsync((CopyClientConfigRequest r) =>
                {
                    var targets = r.ToClients.Select(t => new CopyClientConfigTargetResult
                    {
                        ClientCode = t.ClientCode,
                        ClientName = t.ClientName,
                        StatusCode = failing.ContainsKey(t.ClientCode) ? StatusCode.Error : StatusCode.Success,
                        Message = failing.TryGetValue(t.ClientCode, out var message) ? message : "Copied"
                    }).ToList();
                    var allCopied = targets.All(t => t.StatusCode == StatusCode.Success);

                    return new BaseResponse<CopyClientConfigResponse>
                    {
                        StatusCode = allCopied ? StatusCode.Success : StatusCode.Error,
                        Message = allCopied ? "Success" : "Copied to some clients only.",
                        Result = new CopyClientConfigResponse
                        {
                            Result = allCopied,
                            Targets = targets
                        }
                    };
                });
        }

        private IRenderedComponent<CopyClientSettingsModal> Render()
        {
            var parameters = new ModalParameters();
            parameters.Add("OnCopyClientSettingsCallback", EventCallback.Factory.Create<AlertModel>(this, a => _pageAlerts.Add(a)));

            return Render<CopyClientSettingsModal>(ps => ps.AddCascadingValue(parameters));
        }

        private static List<string> Targets(IRenderedComponent<CopyClientSettingsModal> cut) =>
            cut.FindAll(".dropdown-menu li").Select(li => li.Id).ToList();

        private static void Tick(IRenderedComponent<CopyClientSettingsModal> cut, string clientCode) =>
            cut.Find($"li[id='{clientCode}'] input").Click();

        private static List<string> Chips(IRenderedComponent<CopyClientSettingsModal> cut) =>
            cut.FindAll(".ccs-chip").Select(c => c.TextContent.Trim()).ToList();

        private static AngleSharp.Dom.IElement CopyButton(IRenderedComponent<CopyClientSettingsModal> cut) =>
            cut.FindAll(".modal-footer button").Single(b => b.TextContent.Trim() == "Copy");

        private static void Click(IRenderedComponent<CopyClientSettingsModal> cut, string text) =>
            cut.FindAll(".modal-footer button").Single(b => b.TextContent.Trim() == text).Click();

        private static Task Confirm(IRenderedComponent<CopyClientSettingsModal> cut) =>
            cut.InvokeAsync(() => cut.FindAll(".modal-footer button").Single(b => b.TextContent.Trim() == "Confirm").Click());

        // ---------------------------------------------------------------

        [Fact]
        public void TheFromClient_IsNotOfferedAsATarget()
        {
            var cut = Render();

            Assert.Equal("QTE-A", cut.Find("#fromClient option[selected]").GetAttribute("value"));
            Assert.Equal(new[] { "all", "QTE-B", "QTE-C" }, Targets(cut));
        }

        [Fact]
        public void ChangingFrom_TakesThatClientOutOfTheTargets_AndKeepsTheOtherTicks()
        {
            var cut = Render();
            Tick(cut, "QTE-B");
            Tick(cut, "QTE-C");

            cut.Find("#fromClient").Change("QTE-B");

            Assert.Equal(new[] { "all", "QTE-A", "QTE-C" }, Targets(cut));
            Assert.Equal(new[] { "Client C" }, Chips(cut));
        }

        [Fact]
        public void TickingClients_ShowsTags_AndATagCanBeRemoved()
        {
            var cut = Render();

            Tick(cut, "QTE-B");
            Tick(cut, "QTE-C");
            Assert.Equal(new[] { "Client B", "Client C" }, Chips(cut));

            cut.Find(".ccs-chip button").Click();
            Assert.Equal(new[] { "Client C" }, Chips(cut));
        }

        [Fact]
        public void AllClients_TicksEveryoneButTheFromClient()
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
        }

        [Fact]
        public void WithOneTradingClient_TheDialogOpens_WithNoTargets()
        {
            _userState.Model.TradingClients = Clients("QTE-A");

            var cut = Render();

            Assert.Empty(Targets(cut));
            Assert.Contains("No Clients Available", cut.Markup);
            Assert.True(CopyButton(cut).HasAttribute("disabled"));
        }

        [Fact]
        public void WithTwoTradingClients_TheOtherOneIsTheOnlyTarget()
        {
            _userState.Model.TradingClients = Clients("QTE-A", "QTE-B");

            var cut = Render();

            Assert.Equal(new[] { "all", "QTE-B" }, Targets(cut));
        }

        [Fact]
        public void WithNoTradingClients_TheDialogOpens()
        {
            _userState.Model.TradingClients = new List<PlatformClientResponse>();

            var cut = Render();

            Assert.Empty(Targets(cut));
            Assert.True(CopyButton(cut).HasAttribute("disabled"));
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
        public async Task Confirm_SendsEveryTargetInOneRequest_AndClosesOnSuccess()
        {
            ServerReplies();
            var cut = Render();

            Tick(cut, "QTE-B");
            Tick(cut, "QTE-C");
            Click(cut, "Copy");
            await Confirm(cut);

            var request = Assert.Single(_requests);
            Assert.Equal("QTE-A", request.FromClientCode);
            Assert.Equal("test-user", request.UserId);
            Assert.Equal(
                new[] { ("QTE-B", ClientBId, "Client B"), ("QTE-C", ClientCId, "Client C") },
                request.ToClients.Select(t => (t.ClientCode, t.ClientId, t.ClientName)));

            _modalService.Verify(m => m.Cancel(), Times.Once);
            var alert = Assert.Single(_pageAlerts);
            Assert.Equal(AlertStatus.Success, alert.Status);
            Assert.Contains("2 clients", alert.Message);
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
            Assert.Empty(_pageAlerts);
        }

        [Fact]
        public async Task Confirm_WhenTheSourceHasNothingToCopy_ShowsTheServerMessage()
        {
            _backofficeService
                .Setup(s => s.CopyClientConfig(It.IsAny<CopyClientConfigRequest>()))
                .ReturnsAsync(new BaseResponse<CopyClientConfigResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = "QTE-A has no template assignments to copy.",
                    Result = new CopyClientConfigResponse()
                });
            var cut = Render();

            Tick(cut, "QTE-B");
            Click(cut, "Copy");
            await Confirm(cut);

            Assert.Contains("has no template assignments to copy", cut.Find(".alert-danger").TextContent);
            _modalService.Verify(m => m.Cancel(), Times.Never);
        }
    }
}
