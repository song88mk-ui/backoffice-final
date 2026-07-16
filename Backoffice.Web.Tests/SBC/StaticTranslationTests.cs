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
    // Component tests for the Static Translation page after its restructure to mirror the
    // Dynamic Translation grid: EN is the source column (not a dropdown option), the selected
    // language fills the Translation column, and search supports contains/exact match.
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
                .Setup(s => s.GetSportsbookTranslations("EN"))
                .ReturnsAsync(new BaseResponse<List<SportsbookTranslationResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<SportsbookTranslationResponse>
                    {
                        new SportsbookTranslationResponse { LanguageCode = "EN", Name = "key1", Value = "Hello" },
                        new SportsbookTranslationResponse { LanguageCode = "EN", Name = "key11", Value = "Hello World" },
                        new SportsbookTranslationResponse { LanguageCode = "EN", Name = "key2", Value = "Greeting" }
                    }
                });

            _backofficeService
                .Setup(s => s.GetSportsbookTranslations("ZH"))
                .ReturnsAsync(new BaseResponse<List<SportsbookTranslationResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<SportsbookTranslationResponse>
                    {
                        new SportsbookTranslationResponse { LanguageCode = "ZH", Name = "key1", Value = "你好" },
                        new SportsbookTranslationResponse { LanguageCode = "ZH", Name = "key11", Value = "" },
                        new SportsbookTranslationResponse { LanguageCode = "ZH", Name = "key2", Value = "" }
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
                            new LanguageTranslationStat { LanguageCode = "EN", TotalCount = 3, TranslatedCount = 3 },
                            new LanguageTranslationStat { LanguageCode = "ZH", TotalCount = 3, TranslatedCount = 1 }
                        }
                    }
                });
        }

        [Fact]
        public void LanguageDropdown_HasNoEnglishOption_AndDefaultsToChinese()
        {
            var cut = RenderComponent<StaticTranslation>();

            var options = cut.FindAll("#languageSelect option");
            Assert.DoesNotContain(options, o => o.GetAttribute("value") == "EN");
            Assert.Equal("ZH", options[0].GetAttribute("value"));

            _backofficeService.Verify(s => s.GetSportsbookTranslations("EN"), Times.Once);
            _backofficeService.Verify(s => s.GetSportsbookTranslations("ZH"), Times.Once);
        }

        [Fact]
        public void Render_ShowsLegendBadge_ForTargetLanguages_ButNotForEnglish()
        {
            var cut = RenderComponent<StaticTranslation>();

            var badges = cut.FindAll("span.badge");
            Assert.Contains(badges, b => b.TextContent.Contains("ZH") && b.TextContent.Contains("1/3"));
            Assert.DoesNotContain(badges, b => b.TextContent.Contains("EN"));
        }

        [Fact]
        public void Render_ShowsSourceColumn_AndHighlightsOnlyMissingTranslationCell()
        {
            var cut = RenderComponent<StaticTranslation>();

            var rows = cut.FindAll("tbody tr");
            var missingRow = Assert.Single(rows, r => r.TextContent.Contains("key2"));
            var cells = missingRow.QuerySelectorAll("td");
            Assert.DoesNotContain("table-warning", cells[0].ClassName); // Key cell stays untinted
            Assert.Equal("Greeting", cells[1].TextContent.Trim()); // EN source text is shown
            Assert.DoesNotContain("table-warning", cells[1].ClassName); // source cell stays untinted
            Assert.Contains("table-warning", cells[2].ClassName); // only the Translation cell is highlighted
            Assert.Contains("missing", missingRow.TextContent);

            var translatedRow = Assert.Single(rows, r => r.TextContent.Contains("你好"));
            Assert.DoesNotContain("table-warning", translatedRow.QuerySelectorAll("td")[2].ClassName);
        }

        [Fact]
        public void ShowOnlyUntranslated_FiltersGridToBlankTranslationsOnly()
        {
            var cut = RenderComponent<StaticTranslation>();

            Assert.Equal(3, cut.FindAll("tbody tr").Count);

            cut.Find("#showOnlyUntranslated").Change(true);

            var rows = cut.FindAll("tbody tr");
            Assert.Equal(2, rows.Count);
            Assert.Contains(rows, r => r.TextContent.Contains("key11"));
            Assert.Contains(rows, r => r.TextContent.Contains("key2"));
        }

        [Fact]
        public void ExactMatch_NarrowsSearchFromContainsToEquality()
        {
            var cut = RenderComponent<StaticTranslation>();

            cut.Find("#searchInput").Input("key1");
            Assert.Equal(2, cut.FindAll("tbody tr").Count); // contains: key1 and key11

            cut.Find("#exactMatch").Change(true);

            var row = Assert.Single(cut.FindAll("tbody tr"));
            Assert.Contains("key1", row.TextContent);
            Assert.DoesNotContain("key11", row.TextContent);
        }
    }
}
