using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Pages.SBC;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Backoffice.Web.Tests.SBC
{
    // Component tests for the Static Translation page: the translated/untranslated legend and
    // the "Show only untranslated" filter added on top of the existing key/value grid.
    public class StaticTranslationTests : TestContext
    {
        private readonly Mock<IBackofficeService> _backofficeService = new();

        public StaticTranslationTests()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());
            var translationState = new TranslationState(translationService.Object);

            Services.AddSingleton<IConfiguration>(configuration);
            // CustomModal (rendered at the bottom of the page) casts IModalService to the
            // concrete ModalService and subscribes to its events, so a bare Moq proxy won't do.
            Services.AddSingleton<IModalService>(new ModalService(translationState));
            Services.AddSingleton(new Mock<IPreRenderService>().Object);
            Services.AddSingleton(new Mock<ITradingService>().Object);
            Services.AddSingleton(new Mock<IProviderMappingService>().Object);
            Services.AddSingleton(_backofficeService.Object);
            Services.AddSingleton(new Mock<IAuthenticationService>().Object);
            Services.AddSingleton(new Mock<ITableauService>().Object);
            Services.AddSingleton(new Mock<ICommonService>().Object);
            Services.AddSingleton(translationState);
            Services.AddSingleton<UserState>();
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                new Mock<IAuthenticationService>().Object,
                sp.GetRequiredService<NavigationManager>()));

            var authContext = this.AddTestAuthorization();
            authContext.SetAuthorized("test-user");
            authContext.SetPolicies("translations-static", "translations-static-canedit");

            JSInterop.Setup<string>("getCookie", _ => true).SetResult("EN");

            _backofficeService
                .Setup(s => s.GetSportsbookTranslations(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<List<SportsbookTranslationResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<SportsbookTranslationResponse>
                    {
                        new SportsbookTranslationResponse { LanguageCode = "EN", Name = "key1", Value = "Hello" },
                        new SportsbookTranslationResponse { LanguageCode = "EN", Name = "key2", Value = "" }
                    }
                });

            _backofficeService
                .Setup(s => s.GetSportsbookTranslationStats())
                .ReturnsAsync(new BaseResponse<TranslationStatsResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new TranslationStatsResponse
                    {
                        Languages = new List<LanguageTranslationStat>
                        {
                            new LanguageTranslationStat { LanguageCode = "EN", TotalCount = 2, TranslatedCount = 2 },
                            new LanguageTranslationStat { LanguageCode = "ZH", TotalCount = 2, TranslatedCount = 1 }
                        }
                    }
                });
        }

        [Fact]
        public void Render_ShowsLegendBadge_ForEachLanguage_WithTranslatedCounts()
        {
            var cut = RenderComponent<StaticTranslation>();

            var badges = cut.FindAll("span.badge");
            Assert.Contains(badges, b => b.TextContent.Contains("EN") && b.TextContent.Contains("2/2"));
            Assert.Contains(badges, b => b.TextContent.Contains("ZH") && b.TextContent.Contains("1/2"));
        }

        [Fact]
        public void Render_HighlightsValueCell_AndShowsMissingPlaceholder_ForBlankValue_WithoutTintingOtherCells()
        {
            var cut = RenderComponent<StaticTranslation>();

            var rows = cut.FindAll("tbody tr");
            var missingRow = Assert.Single(rows, r => r.TextContent.Contains("key2"));
            var cells = missingRow.QuerySelectorAll("td");
            Assert.DoesNotContain("table-warning", cells[0].ClassName); // Key cell stays untinted
            Assert.Contains("table-warning", cells[1].ClassName); // only the Value cell is highlighted
            Assert.Contains("missing", missingRow.TextContent);
        }

        [Fact]
        public void ShowOnlyUntranslated_FiltersGridToBlankValuesOnly()
        {
            var cut = RenderComponent<StaticTranslation>();

            Assert.Equal(2, cut.FindAll("tbody tr").Count);

            cut.Find("#showOnlyUntranslated").Change(true);

            var rows = cut.FindAll("tbody tr");
            var row = Assert.Single(rows);
            Assert.Contains("key2", row.TextContent);
        }
    }
}
