using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.OddsEngine.MarketGroups;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text.Json;
using Xunit;

namespace Backoffice.Web.Tests.OddsEngine
{
    // Component tests for the Market Groups Copy dialog (BO-158): pick several target clients, confirm, and
    // one activity log per copied client. The server is a mock; the fan-out itself is covered by
    // ClientMarketTemplateGroupCopyRepositoryTests in the API tests.
    public class MarketGroupCopyModalTests : BunitTestContext
    {
        private static readonly Guid ClientAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid ClientBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid ClientCId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid ClientDId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        private readonly Mock<IBackofficeService> _backofficeService = new();
        private readonly Mock<IModalService> _modalService = new();
        private readonly List<ClientCopyMarketTemplateGroupRequest> _requests = new();
        private readonly List<ActivityLogModel> _logs = new();
        private int _saved;

        public MarketGroupCopyModalTests()
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
                new PlatformClientResponse { ClientId = ClientDId, ClientCode = "QTE-D", ClientName = "Client D" },
            };

            _backofficeService
                .Setup(s => s.AddActivityLog(It.IsAny<ActivityLogModel>()))
                .Callback<ActivityLogModel>(l => _logs.Add(l))
                .ReturnsAsync(new BaseResponse<MessageResponse> { StatusCode = StatusCode.Success });

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

        private static List<ClientMarketTemplateGroupDTO> SourceGroups() => new()
        {
            new ClientMarketTemplateGroupDTO { MarketTemplateGroupId = 2, Name = "Goals", SortOrder = 2, MarketTemplateIds = "3", IsActive = false },
            new ClientMarketTemplateGroupDTO { MarketTemplateGroupId = 1, Name = "Main", SortOrder = 1, MarketTemplateIds = "1,2", IsActive = true },
        };

        private static ClientCopyMarketTemplateGroupTargetResult Copied(Guid clientId) =>
            new ClientCopyMarketTemplateGroupTargetResult
            {
                ClientId = clientId,
                StatusCode = StatusCode.Success,
                Message = "Success",
                PreviousGroups = new List<ClientMarketTemplateGroupDTO> { new ClientMarketTemplateGroupDTO { MarketTemplateGroupId = 50, Name = $"Old {clientId.ToString()[0]}", SortOrder = 1 } },
                CopiedGroups = new List<ClientMarketTemplateGroupDTO>
                {
                    new ClientMarketTemplateGroupDTO { MarketTemplateGroupId = 60, Name = "Main", SortOrder = 1, MarketTemplateIds = "1,2", IsActive = true },
                    new ClientMarketTemplateGroupDTO { MarketTemplateGroupId = 61, Name = "Goals", SortOrder = 2, MarketTemplateIds = "3", IsActive = false },
                }
            };

        private static ClientCopyMarketTemplateGroupTargetResult NotCopied(Guid clientId, string message) =>
            new ClientCopyMarketTemplateGroupTargetResult { ClientId = clientId, StatusCode = StatusCode.Error, Message = message };

        private void ServerReplies(Func<ClientCopyMarketTemplateGroupRequest, ClientCopyMarketTemplateGroupTargetResult> perTarget)
        {
            _backofficeService
                .Setup(s => s.CopyClientMarketTemplateGroup(It.IsAny<ClientCopyMarketTemplateGroupRequest>()))
                .Callback<ClientCopyMarketTemplateGroupRequest>(r => _requests.Add(r))
                .ReturnsAsync((ClientCopyMarketTemplateGroupRequest r) =>
                {
                    var targets = r.CopyToClientIds.Select(id => perTarget(new ClientCopyMarketTemplateGroupRequest { CopyToClientId = id })).ToList();
                    var allCopied = targets.All(t => t.StatusCode == StatusCode.Success);

                    return new BaseResponse<ClientCopyMarketTemplateGroupResponse>
                    {
                        StatusCode = allCopied ? StatusCode.Success : StatusCode.Error,
                        Message = allCopied ? "Success" : "Market groups were copied to some clients only.",
                        Result = new ClientCopyMarketTemplateGroupResponse { ClientId = r.ClientId, SportId = r.SportId, FixtureStatusId = r.FixtureStatusId, Targets = targets }
                    };
                });
        }

        private IRenderedComponent<CopyMarketGroupModal> Render(List<ClientMarketTemplateGroupDTO>? groups = null)
        {
            var parameters = new ModalParameters();
            parameters.Add("ClientId", ClientAId);
            parameters.Add("ClientCode", "QTE-A");
            parameters.Add("SportId", 1);
            parameters.Add("SportName", "Football");
            parameters.Add("FixtureStatusId", (int)FixtureStatus.Live);
            parameters.Add("Groups", groups ?? SourceGroups());

            // The dialog only reads the callback, so a bare CustomModal is enough; rendering the real one needs the page.
#pragma warning disable BL0005
            var modal = new CustomModal { OnSaveClickCallback = EventCallback.Factory.Create<object>(this, _ => _saved++) };
#pragma warning restore BL0005

            return Render<CopyMarketGroupModal>(ps => ps.AddCascadingValue(parameters).AddCascadingValue(modal));
        }

        private static AngleSharp.Dom.IElement ButtonLabelled(IRenderedComponent<CopyMarketGroupModal> cut, string label) =>
            cut.FindAll("button").First(b => b.TextContent.Trim() == label);

        private static void Tick(IRenderedComponent<CopyMarketGroupModal> cut, string value) =>
            cut.Find($"li[id='{value}'] input").Click();

        private static List<string> Tags(IRenderedComponent<CopyMarketGroupModal> cut) =>
            cut.FindAll(".mg-client-chip").Select(c => c.TextContent.Trim()).ToList();

        private static List<string> Offered(IRenderedComponent<CopyMarketGroupModal> cut) =>
            cut.FindAll("li").Select(li => li.Id ?? string.Empty).ToList();

        [Fact]
        public void Opens_ShowingTheSource_ItsGroupsInOrder_AndEveryOtherClient()
        {
            var cut = Render();

            Assert.Contains("Client A", cut.Find(".mg-copy-from").TextContent);
            Assert.Contains("Football > Live", cut.Find(".mg-copy-from").TextContent);
            Assert.Equal(new[] { "Main", "Goals" }, cut.FindAll(".mg-group-chip").Select(c => c.TextContent.Trim()));
            Assert.Equal(new[] { "all", "QTE-B", "QTE-C", "QTE-D" }, Offered(cut));
            Assert.Empty(Tags(cut));
        }

        [Fact]
        public void TickingClients_ShowsThemAsTags_AndRemovingATagUnticksIt()
        {
            var cut = Render();

            Tick(cut, "QTE-B");
            Tick(cut, "QTE-D");
            Assert.Equal(new[] { "Client B", "Client D" }, Tags(cut));

            cut.FindAll(".mg-client-chip").First(c => c.TextContent.Contains("Client B")).QuerySelector("button")!.Click();

            Assert.Equal(new[] { "Client D" }, Tags(cut));
            Assert.False(cut.Find("li[id='QTE-B'] input").HasAttribute("checked"));
            Assert.True(cut.Find("li[id='QTE-D'] input").HasAttribute("checked"));
        }

        [Fact]
        public void AllClients_TicksEveryClientButTheSource()
        {
            var cut = Render();

            Tick(cut, "all");

            Assert.Equal(new[] { "Client B", "Client C", "Client D" }, Tags(cut));
        }

        [Fact]
        public void SavingWithNobodyTicked_AsksForAClient_AndCopiesNothing()
        {
            ServerReplies(r => Copied(r.CopyToClientId));
            var cut = Render();

            ButtonLabelled(cut, "Save").Click();

            Assert.Contains("Select client to proceed", cut.Markup);
            Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Confirm");
            Assert.Empty(_requests);
        }

        [Fact]
        public void ASourceWithoutGroups_CannotBeCopied()
        {
            ServerReplies(r => Copied(r.CopyToClientId));
            var cut = Render(new List<ClientMarketTemplateGroupDTO>());
            Tick(cut, "QTE-B");

            Assert.Contains("has no saved market groups", cut.Markup);
            Assert.True(ButtonLabelled(cut, "Save").HasAttribute("disabled"));
            Assert.Empty(_requests);
        }

        [Fact]
        public void TheConfirmStep_SaysHowManyClientsAreReplaced_AndBackReturnsToTheSelection()
        {
            var cut = Render();
            Tick(cut, "QTE-B");
            Tick(cut, "QTE-C");

            ButtonLabelled(cut, "Save").Click();

            var warning = cut.Find(".alert-warning").TextContent;
            Assert.Contains("2 clients", warning);
            Assert.Contains("Client B, Client C", warning);
            Assert.Contains("Football > Live", warning);
            Assert.Contains("2 market groups", warning);

            ButtonLabelled(cut, "Back").Click();

            Assert.Equal(new[] { "Client B", "Client C" }, Tags(cut));
            Assert.Empty(_requests);
        }

        [Fact]
        public void Confirming_SendsOneRequestForEveryTarget_LogsEachClient_AndCloses()
        {
            ServerReplies(r => Copied(r.CopyToClientId));
            var cut = Render();
            Tick(cut, "QTE-B");
            Tick(cut, "QTE-C");
            ButtonLabelled(cut, "Save").Click();

            ButtonLabelled(cut, "Confirm").Click();

            cut.WaitForAssertion(() => _modalService.Verify(m => m.Cancel(), Times.Once()), TimeSpan.FromSeconds(5));

            var request = Assert.Single(_requests);
            Assert.Equal((ClientAId, 1, (int)FixtureStatus.Live), (request.ClientId, request.SportId, request.FixtureStatusId));
            Assert.Equal((1, (int)FixtureStatus.Live), (request.CopyToSportId, request.CopyToFixtureStatusId));
            Assert.Equal(new[] { ClientBId, ClientCId }, request.CopyToClientIds);

            Assert.Equal(new[] { "QTE-B.1.1.0", "QTE-C.1.1.0" }, _logs.Select(l => l.Payload));
            Assert.All(_logs, l =>
            {
                Assert.Equal("SBC.OE.MarketSetting.Group", l.Page);
                Assert.Equal("Copy", l.Activity);
                Assert.Equal("test-user", l.UserId);
                Assert.Equal(new[] { "Main", "Goals" }, JsonSerializer.Deserialize<List<MarketGroupActivtyModel>>(l.To)!.Select(g => g.Name));
            });
            Assert.Equal("Old b", JsonSerializer.Deserialize<List<MarketGroupActivtyModel>>(_logs[0].From)!.Single().Name);
            Assert.Equal(1, _saved);
        }

        [Fact]
        public void APartialFailure_NamesTheCopiedAndNotCopiedClients_AndLogsOnlyTheCopiedOnes()
        {
            ServerReplies(r => r.CopyToClientId == ClientCId ? NotCopied(ClientCId, "Error") : Copied(r.CopyToClientId));
            var cut = Render();
            Tick(cut, "all");
            ButtonLabelled(cut, "Save").Click();

            ButtonLabelled(cut, "Confirm").Click();

            cut.WaitForAssertion(() => Assert.Contains("Copied to: Client B, Client D. Not copied: Client C (Error).", cut.Markup));
            Assert.Equal(new[] { "QTE-B.1.1.0", "QTE-D.1.1.0" }, _logs.Select(l => l.Payload));
            _modalService.Verify(m => m.Cancel(), Times.Never());
            Assert.Equal(0, _saved);

            // Back on the selection, with only the client that was not copied still ticked.
            Assert.Equal(new[] { "Client C" }, Tags(cut));
            Assert.Contains(cut.FindAll("button"), b => b.TextContent.Trim() == "Save");
        }

        [Fact]
        public void ARejectedRequest_ShowsTheServerMessage_AndLogsNothing()
        {
            _backofficeService
                .Setup(s => s.CopyClientMarketTemplateGroup(It.IsAny<ClientCopyMarketTemplateGroupRequest>()))
                .ReturnsAsync(new BaseResponse<ClientCopyMarketTemplateGroupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = "Source client has no market groups to copy",
                    Result = new ClientCopyMarketTemplateGroupResponse()
                });
            var cut = Render();
            Tick(cut, "QTE-B");
            ButtonLabelled(cut, "Save").Click();

            ButtonLabelled(cut, "Confirm").Click();

            cut.WaitForAssertion(() => Assert.Contains("Not copied: Client B (Source client has no market groups to copy).", cut.Markup));
            Assert.Empty(_logs);
        }
    }
}
