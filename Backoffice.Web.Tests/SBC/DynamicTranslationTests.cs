using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
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
    // Component tests for the Dynamic Translation page: the translated/untranslated legend and
    // the server-side "Show only untranslated" filter added on top of the existing glossary grid.
    public class DynamicTranslationTests : TestContext
    {
        private readonly Mock<IBackofficeService> _backofficeService = new();
        private GetSportsbookGlossaryRequest? _lastRequest;

        public DynamicTranslationTests()
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
            authContext.SetPolicies("translations-dynamic", "translations-dynamic-canedit");

            JSInterop.Setup<string>("getCookie", _ => true).SetResult("EN");

            _backofficeService
                .Setup(s => s.GetSportsbookGlossary(It.IsAny<GetSportsbookGlossaryRequest>()))
                .Callback<GetSportsbookGlossaryRequest>(r => _lastRequest = r)
                .ReturnsAsync(new BaseResponse<SportsbookGlossaryListResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new SportsbookGlossaryListResponse
                    {
                        Glossary = new List<SportsbookGlossaryResponse>
                        {
                            new SportsbookGlossaryResponse { Id = 1, StringId = "SpId:1", SourceText = "Soccer", TargetText = "足球", TargetLanguage = "ZH" },
                            new SportsbookGlossaryResponse { Id = 2, StringId = "SpId:2", SourceText = "Tennis", TargetText = "Tennis", TargetLanguage = "ZH" },
                            new SportsbookGlossaryResponse { Id = 3, StringId = "SeTe:3", SourceText = "5+", TargetText = "", TargetLanguage = "ZH" }
                        },
                        MetaData = new MetaData { CurrentPage = 1, PageSize = 10, TotalCount = 3, TotalPages = 1 }
                    }
                });

            _backofficeService
                .Setup(s => s.GetSportsbookGlossaryStats())
                .ReturnsAsync(new BaseResponse<TranslationStatsResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new TranslationStatsResponse
                    {
                        Languages = new List<LanguageTranslationStat>
                        {
                            new LanguageTranslationStat { LanguageCode = "ZH", TotalCount = 2, TranslatedCount = 1 },
                            new LanguageTranslationStat { LanguageCode = "ES", TotalCount = 2, TranslatedCount = 2 },
                            new LanguageTranslationStat { LanguageCode = "FR", TotalCount = 2, TranslatedCount = 2 }
                        }
                    }
                });
        }

        [Fact]
        public void Render_ShowsLegendBadge_ForEachTargetLanguage_WithTranslatedCounts()
        {
            var cut = RenderComponent<DynamicTranslation>();

            var badges = cut.FindAll("span.badge");
            Assert.Contains(badges, b => b.TextContent.Contains("ZH") && b.TextContent.Contains("1/2"));
            Assert.Contains(badges, b => b.TextContent.Contains("ES") && b.TextContent.Contains("2/2"));
            Assert.Contains(badges, b => b.TextContent.Contains("FR") && b.TextContent.Contains("2/2"));
        }

        [Fact]
        public void Render_HighlightsTranslationCell_AndShowsMissingPlaceholder_WhenTargetEqualsSource_WithoutTintingActionsColumn()
        {
            var cut = RenderComponent<DynamicTranslation>();
            cut.Find("#languageSelect").Change("ZH"); // missing-placeholder logic only applies to non-EN target languages

            var rows = cut.FindAll("tbody tr");
            var missingRow = Assert.Single(rows, r => r.TextContent.Contains("SpId:2"));
            var missingCells = missingRow.QuerySelectorAll("td");
            Assert.DoesNotContain("table-warning", missingCells[0].ClassName); // StringId cell stays untinted
            Assert.Contains("table-warning", missingCells[2].ClassName); // only the Translation cell is highlighted
            Assert.DoesNotContain("table-warning", missingCells[4].ClassName); // actions column (icons) stays untinted
            Assert.Contains("missing", missingRow.TextContent);

            var translatedRow = Assert.Single(rows, r => r.TextContent.Contains("SpId:1"));
            Assert.DoesNotContain("table-warning", translatedRow.QuerySelectorAll("td")[2].ClassName);
        }

        [Fact]
        public void Render_DoesNotFlagLetterlessSourceText_AsUntranslated_AndShowsItsValue()
        {
            var cut = RenderComponent<DynamicTranslation>();
            cut.Find("#languageSelect").Change("ZH");

            var rows = cut.FindAll("tbody tr");
            var numericRow = Assert.Single(rows, r => r.TextContent.Contains("SeTe:3"));
            var cells = numericRow.QuerySelectorAll("td");
            Assert.DoesNotContain("table-warning", cells[2].ClassName); // "5+" reads the same in every language
            Assert.DoesNotContain("missing", numericRow.TextContent);
            Assert.Equal("5+", cells[2].TextContent.Trim()); // blank target falls back to the source text
        }

        [Fact]
        public void ShowOnlyUntranslated_SendsUntranslatedOnlyFlag_ToTheApi()
        {
            var cut = RenderComponent<DynamicTranslation>();
            cut.Find("#languageSelect").Change("ZH"); // the flag is only meaningful once a target language is selected

            cut.Find("#showOnlyUntranslated").Change(true);

            Assert.NotNull(_lastRequest);
            Assert.True(_lastRequest!.UntranslatedOnly);
        }

        [Fact]
        public void LanguageDropdown_HasNoEnglishOption_AndDefaultsToChinese()
        {
            var cut = RenderComponent<DynamicTranslation>();

            var options = cut.FindAll("#languageSelect option");
            Assert.DoesNotContain(options, o => o.GetAttribute("value") == "EN");
            Assert.Equal("ZH", options[0].GetAttribute("value"));

            Assert.NotNull(_lastRequest);
            Assert.Equal("ZH", _lastRequest!.TargetLanguage);
        }

        [Fact]
        public void ShowOnlyUntranslated_CheckboxIsAlwaysEnabled()
        {
            var cut = RenderComponent<DynamicTranslation>();

            var checkbox = cut.Find("#showOnlyUntranslated");
            Assert.False(checkbox.HasAttribute("disabled"));
        }

        [Fact]
        public void ExactMatch_SendsExactMatchFlag_AndResetsToFirstPage()
        {
            var cut = RenderComponent<DynamicTranslation>();

            cut.Find("#exactMatch").Change(true);

            Assert.NotNull(_lastRequest);
            Assert.True(_lastRequest!.ExactMatch);
            Assert.Equal(1, _lastRequest.MetaData.CurrentPage);
        }

        [Fact]
        public void ExactMatch_DefaultsToFalse_OnInitialLoad()
        {
            var cut = RenderComponent<DynamicTranslation>();

            Assert.NotNull(_lastRequest);
            Assert.False(_lastRequest!.ExactMatch);
            Assert.False(cut.Find("#exactMatch").HasAttribute("checked"));
        }
    }
}
