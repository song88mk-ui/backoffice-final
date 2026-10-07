using Backoffice.Web.Models;
using Backoffice.Web.Pages.SBC;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Backoffice.Web.Tests.SBC
{
    // Component tests for the Processed-flag confirmation modal. Every component in
    // Backoffice.Web receives all _Imports.razor @inject services, so the fixture registers
    // the full set even though the modal itself only uses IModalService.
    public class DynamicTranslationProcessedModalTests : BunitTestContext
    {
        private readonly Mock<IModalService> _modalService = new();

        public DynamicTranslationProcessedModalTests()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            Services.AddSingleton<IConfiguration>(configuration);
            Services.AddSingleton(_modalService.Object);
            Services.AddSingleton(new Mock<IPreRenderService>().Object);
            Services.AddSingleton(new Mock<ITradingService>().Object);
            Services.AddSingleton(new Mock<IProviderMappingService>().Object);
            Services.AddSingleton(new Mock<IBackofficeService>().Object);
            Services.AddSingleton(new Mock<IAuthenticationService>().Object);
            Services.AddSingleton(new Mock<ITableauService>().Object);
            Services.AddSingleton(new Mock<ICommonService>().Object);
            Services.AddSingleton(new TranslationState(new Mock<ITranslationService>().Object));
            Services.AddSingleton<UserState>();
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                new Mock<IAuthenticationService>().Object,
                sp.GetRequiredService<NavigationManager>()));
        }

        private IRenderedComponent<DynamicTranslationProcessedModal> Render(
            bool processed, Func<Task<bool>>? onConfirm = null, string stringId = "LeId:1050")
        {
            var parameters = new ModalParameters();
            parameters.Add("StringId", stringId);
            parameters.Add("Processed", processed);
            parameters.Add("OnConfirmCallback", onConfirm ?? (() => Task.FromResult(true)));

            return Render<DynamicTranslationProcessedModal>(ps => ps.AddCascadingValue(parameters));
        }

        [Fact]
        public void ProcessedTerm_ShowsRequeueWording()
        {
            var cut = Render(processed: true);

            Assert.Contains("Re-queue", cut.Markup);
            Assert.Contains("LeId:1050", cut.Markup);
            Assert.Contains("worklist", cut.Markup);
            Assert.Contains("may overwrite", cut.Markup);
            Assert.Equal("Re-queue", cut.Find("button.btn-primary").TextContent.Trim());
        }

        [Fact]
        public void PendingTerm_ShowsMarkProcessedWording()
        {
            var cut = Render(processed: false);

            Assert.Contains("Mark", cut.Markup);
            Assert.Contains("LeId:1050", cut.Markup);
            Assert.Contains("skip this term", cut.Markup);
            Assert.Equal("Mark Processed", cut.Find("button.btn-primary").TextContent.Trim());
        }

        [Fact]
        public void Confirm_InvokesCallback_AndClosesModal_OnSuccess()
        {
            var invocations = 0;
            var cut = Render(processed: true, onConfirm: () =>
            {
                invocations++;
                return Task.FromResult(true);
            });

            cut.Find("button.btn-primary").Click();

            Assert.Equal(1, invocations);
            _modalService.Verify(m => m.Close(), Times.Once);
            _modalService.Verify(m => m.Cancel(), Times.Never);
        }

        [Fact]
        public void Confirm_DoesNotCloseModal_WhenCallbackFails()
        {
            var cut = Render(processed: false, onConfirm: () => Task.FromResult(false));

            cut.Find("button.btn-primary").Click();

            _modalService.Verify(m => m.Close(), Times.Never);
        }

        [Fact]
        public void Cancel_CancelsModal_WithoutInvokingCallback()
        {
            var invocations = 0;
            var cut = Render(processed: true, onConfirm: () =>
            {
                invocations++;
                return Task.FromResult(true);
            });

            cut.Find("button.btn-secondary").Click();

            Assert.Equal(0, invocations);
            _modalService.Verify(m => m.Cancel(), Times.Once);
            _modalService.Verify(m => m.Close(), Times.Never);
        }
    }
}
