using Backoffice.Shared.Enums;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Moq;
using Xunit;

namespace Backoffice.Web.Tests.Common
{
    public class ConfirmDialogTests : BunitTestContext
    {
        public ConfirmDialogTests()
        {
            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            Services.AddSingleton<IConfiguration>(configuration);
            Services.AddSingleton(new Mock<IPreRenderService>().Object);
            Services.AddSingleton(new TranslationState(translationService.Object));
            Services.AddSingleton<UserState>();
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(new Mock<IModalService>().Object);
            Services.AddSingleton(new Mock<ITradingService>().Object);
            Services.AddSingleton(new Mock<IProviderMappingService>().Object);
            Services.AddSingleton(new Mock<IBackofficeService>().Object);
            Services.AddSingleton(new Mock<IAuthenticationService>().Object);
            Services.AddSingleton(new Mock<ITableauService>().Object);
            Services.AddSingleton(new Mock<ICommonService>().Object);
            Services.AddSingleton(sp => new PageState(
                new Mock<IAuthenticationService>().Object,
                sp.GetRequiredService<NavigationManager>()));
        }

        [Fact]
        public void NotVisible_RendersNothing()
        {
            var cut = Render<ConfirmDialog>(p => p.Add(d => d.Visible, false));

            Assert.Empty(cut.Markup.Trim());
        }

        [Fact]
        public void Visible_ShowsMessageAndButtonLabels()
        {
            var cut = Render<ConfirmDialog>(p => p
                .Add(d => d.Visible, true)
                .Add(d => d.Message, "Discard changes?")
                .Add(d => d.CancelText, "Keep Editing")
                .Add(d => d.ConfirmText, "Discard"));

            Assert.Equal("Discard changes?", cut.Find(".confirm-dialog-message").TextContent);
            Assert.Equal("Keep Editing", cut.Find(".btn-dismiss").TextContent);
            Assert.Equal("Discard", cut.Find(".btn-confirm").TextContent);
            Assert.NotNull(cut.Find(".confirm-dialog-backdrop"));
        }

        [Fact]
        public void ClickingButtons_InvokesMatchingCallback()
        {
            var confirmed = 0;
            var cancelled = 0;
            var cut = Render<ConfirmDialog>(p => p
                .Add(d => d.Visible, true)
                .Add(d => d.OnConfirm, () => confirmed++)
                .Add(d => d.OnCancel, () => cancelled++));

            cut.Find(".btn-dismiss").Click();
            Assert.Equal((0, 1), (confirmed, cancelled));

            cut.Find(".btn-confirm").Click();
            Assert.Equal((1, 1), (confirmed, cancelled));
        }
    }
}
