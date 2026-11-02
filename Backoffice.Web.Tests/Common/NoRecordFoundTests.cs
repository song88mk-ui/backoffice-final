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
    // BO-151: an environment with no translation rows (DEV) used to show the raw key "no_record_message" under the heading.
    public class NoRecordFoundTests : BunitTestContext
    {
        private const string English = "Try adjusting your search or filter to find what you're looking for.";

        private void Register(Dictionary<string, string> translations)
        {
            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(translations);

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
        public void WithoutATranslation_ShowsTheEnglishSentence_NotTheKey()
        {
            Register(new Dictionary<string, string>());

            var cut = Render<NoRecordFound>();

            Assert.Equal(English, cut.Find("p").TextContent);
            Assert.DoesNotContain("no_record_message", cut.Markup);
        }

        [Fact]
        public void WithATranslation_ShowsTheTranslation()
        {
            Register(new Dictionary<string, string> { ["no_record_message"] = "원하는 정보를 찾으려면 검색 또는 필터를 조정하세요." });

            var cut = Render<NoRecordFound>();

            Assert.Equal("원하는 정보를 찾으려면 검색 또는 필터를 조정하세요.", cut.Find("p").TextContent);
        }

        [Fact]
        public void TheHeading_StillShowsAGivenMessage()
        {
            Register(new Dictionary<string, string>());

            var cut = Render<NoRecordFound>(p => p.Add(c => c.Message, "No Templates"));

            Assert.Equal("No Templates!", cut.Find("h4").TextContent.Trim());
        }
    }
}
