using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.OddsEngine.MarketSortOrder;
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
    // Component tests for the Market Sort Order Copy dialog (BO-159): pick several target clients, a sport and a
    // scope, confirm, and report which clients were copied. The server is a mock; the per-target copy itself is
    // covered by ClientMarketSortOrderCopyRepositoryTests in the API tests.
    public class MarketSortOrderCopyModalTests : BunitTestContext
    {
        private static readonly Guid ClientAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid ClientBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid ClientCId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        private readonly Mock<IBackofficeService> _backofficeService = new();
        private readonly Mock<IModalService> _modalService = new();
        private readonly List<CopyClientMarketSortOrderRequest> _requests = new();
        private int _saved;

        public MarketSortOrderCopyModalTests()
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
                new PlatformClientResponse { ClientId = ClientAId, ClientCode = "QTE-A", ClientName = "Client A" },
                new PlatformClientResponse { ClientId = ClientBId, ClientCode = "QTE-B", ClientName = "Client B" },
                new PlatformClientResponse { ClientId = ClientCId, ClientCode = "QTE-C", ClientName = "Client C" },
            };

            _backofficeService
                .Setup(s => s.GetSportLookup(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<SportsLookupResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new SportsLookupResponse
                    {
                        SportLookup = new List<SportLookupModel>
                        {
                            new SportLookupModel { SportId = 1, SportName = "Football" },
                            new SportLookupModel { SportId = 2, SportName = "Basketball" }
                        }
                    }
                });

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

        // The server copies every target, except those listed in failing (with that message).
        private void ServerReplies(Dictionary<Guid, string>? failing = null)
        {
            failing ??= new Dictionary<Guid, string>();

            _backofficeService
                .Setup(s => s.CopyClientMarketSortOrder(It.IsAny<CopyClientMarketSortOrderRequest>()))
                .Callback<CopyClientMarketSortOrderRequest>(r => _requests.Add(r))
                .ReturnsAsync((CopyClientMarketSortOrderRequest r) =>
                {
                    var targets = r.CopyToClientIds.Select(id => new CopyClientMarketSortOrderTargetResult
                    {
                        ClientId = id,
                        StatusCode = failing.ContainsKey(id) ? StatusCode.Error : StatusCode.Success,
                        Message = failing.TryGetValue(id, out var message) ? message : "Copied"
                    }).ToList();
                    var allCopied = targets.All(t => t.StatusCode == StatusCode.Success);

                    return new BaseResponse<CopyClientMarketSortOrderResponse>
                    {
                        StatusCode = allCopied ? StatusCode.Success : StatusCode.Error,
                        Message = allCopied ? "Copy market sort order successful" : "Market sort order copied to some clients",
                        Result = new CopyClientMarketSortOrderResponse { Targets = targets }
                    };
                });
        }

        // Source on screen: Client A, Football, Pre-match.
        private IRenderedComponent<CopyModal> Render()
        {
            var parameters = new ModalParameters();
            parameters.Add("FromClientCode", "QTE-A");
            parameters.Add("FromSportId", 1);
            parameters.Add("FromSportName", "Football");
            parameters.Add("FromFixtureStatusId", (int)Scope.Prematch);
            parameters.Add("FromFixtureStatusName", "Prematch");

            // The dialog only reads the callback, so a bare CustomModal is enough; rendering the real one needs the page.
#pragma warning disable BL0005
            var modal = new CustomModal { OnSaveClickCallback = EventCallback.Factory.Create<object>(this, _ => _saved++) };
#pragma warning restore BL0005

            return Render<CopyModal>(ps => ps.AddCascadingValue(parameters).AddCascadingValue(modal));
        }

        private static void Tick(IRenderedComponent<CopyModal> cut, string clientCode) =>
            cut.Find($"li[id='{clientCode}'] input").Click();

        private static void Choose(IRenderedComponent<CopyModal> cut, int sportId, Scope scope)
        {
            cut.Find("#market-sort-order-copy-sport").Change(sportId.ToString());
            cut.Find("#market-sort-order-copy-scope").Change(((int)scope).ToString());
        }

        private static List<string> Chips(IRenderedComponent<CopyModal> cut) =>
            cut.FindAll(".mso-chip").Select(c => c.TextContent.Trim()).ToList();

        private static AngleSharp.Dom.IElement CopyButton(IRenderedComponent<CopyModal> cut) =>
            cut.FindAll(".modal-footer button").Single(b => b.TextContent.Trim() == "Copy");

        private static void Click(IRenderedComponent<CopyModal> cut, string text) =>
            cut.FindAll(".modal-footer button").Single(b => b.TextContent.Trim() == text).Click();

        // ---------------------------------------------------------------

        [Fact]
        public void ListsEveryClient_IncludingTheSource_UnderAllClients()
        {
            var cut = Render();

            var items = cut.FindAll(".dropdown-menu li").Select(li => li.Id).ToList();

            Assert.Equal(new[] { "all", "QTE-A", "QTE-B", "QTE-C" }, items);
        }

        [Fact]
        public void TickingClients_ShowsTags_AndATagCanBeRemoved()
        {
            var cut = Render();

            Tick(cut, "QTE-B");
            Tick(cut, "QTE-C");
            Assert.Equal(new[] { "Client B", "Client C" }, Chips(cut));

            cut.Find(".mso-chip button").Click();
            Assert.Equal(new[] { "Client C" }, Chips(cut));
        }

        [Fact]
        public void AllClients_TicksEveryone()
        {
            var cut = Render();

            Tick(cut, "all");

            Assert.Equal(new[] { "Client A", "Client B", "Client C" }, Chips(cut));
        }

        [Fact]
        public void Copy_IsDisabledUntilAClientASportAndAScopeAreChosen()
        {
            var cut = Render();
            Assert.True(CopyButton(cut).HasAttribute("disabled"));

            Tick(cut, "QTE-B");
            Assert.True(CopyButton(cut).HasAttribute("disabled"));

            cut.Find("#market-sort-order-copy-sport").Change("1");
            Assert.True(CopyButton(cut).HasAttribute("disabled"));

            cut.Find("#market-sort-order-copy-scope").Change(((int)Scope.Live).ToString());
            Assert.False(CopyButton(cut).HasAttribute("disabled"));
        }

        [Fact]
        public void SameClientSportAndScope_IsRefusedWithAMessage()
        {
            ServerReplies();
            var cut = Render();

            Tick(cut, "QTE-A");
            Tick(cut, "QTE-B");
            Choose(cut, 1, Scope.Prematch);
            Click(cut, "Copy");

            Assert.Contains("same client, sport, and scope", cut.Markup);
            Assert.Contains("Client A", cut.Find(".alert-danger").TextContent);
            Assert.Empty(cut.FindAll("button").Where(b => b.TextContent.Trim() == "Confirm"));
            Assert.Empty(_requests);
        }

        [Fact]
        public void TheSourceClient_IsAllowedForAnotherScope()
        {
            var cut = Render();

            Tick(cut, "QTE-A");
            Choose(cut, 1, Scope.Live);
            Click(cut, "Copy");

            Assert.Contains("Do you want to continue?", cut.Markup);
        }

        [Fact]
        public void Confirmation_SaysHowManyClientsWillBeReplaced()
        {
            var cut = Render();

            Tick(cut, "QTE-B");
            Tick(cut, "QTE-C");
            Choose(cut, 2, Scope.Live);
            Click(cut, "Copy");

            var warning = cut.Find(".alert-warning").TextContent;
            Assert.Contains("2 clients", warning);
            Assert.Contains("Basketball", warning);
            Assert.Contains("Live", warning);
            Assert.Contains("Client A", warning);
        }

        [Fact]
        public async Task Confirm_SendsEveryTarget_AndClosesOnSuccess()
        {
            ServerReplies();
            var cut = Render();

            Tick(cut, "QTE-B");
            Tick(cut, "QTE-C");
            Choose(cut, 2, Scope.Live);
            Click(cut, "Copy");
            await cut.InvokeAsync(() => cut.FindAll(".modal-footer button").Single(b => b.TextContent.Trim() == "Confirm").Click());

            var request = Assert.Single(_requests);
            Assert.Equal(ClientAId, request.CopyFromClientId);
            Assert.Equal(1, request.CopyFromSportId);
            Assert.Equal((int)Scope.Prematch, request.CopyFromFixtureStatusId);
            Assert.Equal(new[] { ClientBId, ClientCId }, request.CopyToClientIds);
            Assert.Equal(2, request.CopyToSportId);
            Assert.Equal((int)Scope.Live, request.CopyToFixtureStatusId);
            Assert.Equal("test-user", request.UserId);
            Assert.Equal("Test User", request.UserName);

            cut.WaitForAssertion(() => _modalService.Verify(m => m.Cancel(), Times.Once), TimeSpan.FromSeconds(5));
            Assert.Equal(1, _saved);
        }

        [Fact]
        public async Task Confirm_WhenOneClientFails_SaysWhichWereCopiedAndWhichWereNot()
        {
            ServerReplies(new Dictionary<Guid, string> { [ClientCId] = "database unavailable" });
            var cut = Render();

            Tick(cut, "QTE-B");
            Tick(cut, "QTE-C");
            Choose(cut, 1, Scope.Live);
            Click(cut, "Copy");
            await cut.InvokeAsync(() => cut.FindAll(".modal-footer button").Single(b => b.TextContent.Trim() == "Confirm").Click());

            var alert = cut.Find(".alert-danger").TextContent;
            Assert.Contains("Copied to Client B", alert);
            Assert.Contains("Not copied to Client C (database unavailable)", alert);
            _modalService.Verify(m => m.Cancel(), Times.Never);
            Assert.Equal(1, _saved);
        }
    }
}
