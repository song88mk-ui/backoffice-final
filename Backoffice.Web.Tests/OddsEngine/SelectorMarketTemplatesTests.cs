using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Pages.OddsEngine;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Backoffice.Web.Tests.OddsEngine
{
    // Component tests for the Selector Market Templates page: load-time ordering,
    // the inline (tfoot) Add Row control, and the per-sport template dropdown lookup.
    public class SelectorMarketTemplatesTests : BunitTestContext
    {
        private static readonly Guid ClientId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private readonly Mock<IBackofficeService> _backofficeService = new();

        public SelectorMarketTemplatesTests()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());
            var translationState = new TranslationState(translationService.Object);

            var authenticationService = new Mock<IAuthenticationService>();
            authenticationService
                .Setup(s => s.AuthenticateToken(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<TokenModel> { StatusCode = StatusCode.Success });

            var userState = new UserState();
            userState.Model.Id = "test-user";
            userState.Model.TradingClients.Add(new PlatformClientResponse
            {
                ClientId = ClientId,
                ClientCode = "GSA",
                ClientName = "GameSnap LiboAsia"
            });
            userState.Model.Claims.Add(new Claim("SBC.OE.MarketSetting.SelectorTemplates", "1"));

            Services.AddSingleton<IConfiguration>(configuration);
            Services.AddSingleton<IModalService>(new ModalService(translationState));
            Services.AddSingleton(new Mock<IPreRenderService>().Object);
            Services.AddSingleton(new Mock<ITradingService>().Object);
            Services.AddSingleton(new Mock<IProviderMappingService>().Object);
            Services.AddSingleton(_backofficeService.Object);
            Services.AddSingleton(authenticationService.Object);
            Services.AddSingleton(new Mock<ITableauService>().Object);
            Services.AddSingleton(new Mock<ICommonService>().Object);
            Services.AddSingleton(translationState);
            Services.AddSingleton(userState);
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                authenticationService.Object,
                sp.GetRequiredService<NavigationManager>()));

            var authContext = this.AddAuthorization();
            authContext.SetAuthorized("test-user");
            authContext.SetPolicies("sbc-market-selectortemplates", "sbc-market-selectortemplates-canedit");

            JSInterop.Mode = JSRuntimeMode.Loose;

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

            _backofficeService
                .Setup(s => s.GetSelectorMarketTemplateLookup())
                .ReturnsAsync(new BaseResponse<List<SelectorMarketTemplateLookupModel>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<SelectorMarketTemplateLookupModel>
                    {
                        new SelectorMarketTemplateLookupModel { SportId = 1, MarketTemplateId = "1001", MarketTemplateCode = "ML", SortOrder = 1 },
                        new SelectorMarketTemplateLookupModel { SportId = 1, MarketTemplateId = "1002", MarketTemplateCode = "O/U", SortOrder = 2 },
                        new SelectorMarketTemplateLookupModel { SportId = 2, MarketTemplateId = "2001", MarketTemplateCode = "HDP", SortOrder = 1 }
                    }
                });

            // Returned deliberately unsorted: LoadTemplates must order by sport then Asia sort order.
            // Euro sort orders are deliberately unrelated to the Asia ones so the renumbering tests
            // can tell "renumbered to follow the row order" apart from "left untouched".
            _backofficeService
                .Setup(s => s.GetClientSelectorMarketTemplates(ClientId))
                .ReturnsAsync(new BaseResponse<List<ClientSelectorMarketTemplateModel>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<ClientSelectorMarketTemplateModel>
                    {
                        new ClientSelectorMarketTemplateModel { SportId = 2, MarketTemplateId = "2001", MarketTemplateCode = "HDP", AsiaSortOrder = 1, EuroSortOrder = 7, Identifier = "FT" },
                        new ClientSelectorMarketTemplateModel { SportId = 1, MarketTemplateId = "1002", MarketTemplateCode = "O/U", AsiaSortOrder = 2, EuroSortOrder = 8, Identifier = "FT" },
                        new ClientSelectorMarketTemplateModel { SportId = 1, MarketTemplateId = "1001", MarketTemplateCode = "ML", AsiaSortOrder = 1, EuroSortOrder = 9, Identifier = "FT" }
                    }
                });
        }

        // Asia is the first number input of a row, Euro the second.
        private static List<string?> AsiaSortOrders(IEnumerable<AngleSharp.Dom.IElement> rows) =>
            rows.Select(r => r.QuerySelectorAll("input[type=number]")[0].GetAttribute("value")).ToList();

        private static List<string?> EuroSortOrders(IEnumerable<AngleSharp.Dom.IElement> rows) =>
            rows.Select(r => r.QuerySelectorAll("input[type=number]")[1].GetAttribute("value")).ToList();

        private IRenderedComponent<SelectorMarketTemplates> RenderPageWithClientSelected()
        {
            var cut = Render<SelectorMarketTemplates>();

            cut.Find("select").Change("GSA");
            cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("tbody tr")));

            return cut;
        }

        [Fact]
        public void RowsRenderSortedBySportThenAsiaSortOrder()
        {
            var cut = RenderPageWithClientSelected();

            var rows = cut.FindAll("tbody tr");
            Assert.Equal(3, rows.Count);

            var sportValues = rows.Select(r => r.QuerySelector("select")!.GetAttribute("value")).ToList();
            Assert.Equal(new[] { "1", "1", "2" }, sportValues);

            var templateCodes = rows.Select(r => r.QuerySelectorAll("td")[2].TextContent.Trim()).ToList();
            Assert.Equal(new[] { "ML", "O/U", "HDP" }, templateCodes);

            // Loading must not renumber: the stored Euro sort orders survive untouched.
            Assert.Equal(new[] { "9", "8", "7" }, EuroSortOrders(rows));
        }

        [Fact]
        public void AddRowAppendsNewRowAtBottomWithoutResorting()
        {
            var cut = RenderPageWithClientSelected();

            cut.Find("tfoot button").Click();

            var rows = cut.FindAll("tbody tr");
            Assert.Equal(4, rows.Count);

            // The new row (SportId 0) must stay at the bottom; with per-render sorting it
            // would jump to the top of the table.
            var sportValues = rows.Select(r => r.QuerySelector("select")!.GetAttribute("value")).ToList();
            Assert.Equal(new[] { "1", "1", "2", "0" }, sportValues);
        }

        [Fact]
        public void InsertRowBelowPrefillsSportAndRenumbersSortOrders()
        {
            var cut = RenderPageWithClientSelected();

            // Insert below the first row (sport 1, ML, Asia 1).
            cut.FindAll("tbody .insert-row")[0].Click();

            var rows = cut.FindAll("tbody tr");
            Assert.Equal(4, rows.Count);

            // New row sits directly under its source with the same sport and no template yet.
            var newRow = rows[1];
            Assert.Equal("1", newRow.QuerySelector("select")!.GetAttribute("value"));
            Assert.Equal("", newRow.QuerySelectorAll("select")[1].GetAttribute("value"));

            // Both sort orders renumbered 1..n within each sport: ML, new, O/U then HDP.
            Assert.Equal(new[] { "1", "2", "3", "1" }, AsiaSortOrders(rows));
            Assert.Equal(new[] { "1", "2", "3", "1" }, EuroSortOrders(rows));
        }

        [Fact]
        public async Task UpdateSelectorTemplateOrderReordersRowsAndRenumbersPerSport()
        {
            var cut = RenderPageWithClientSelected();

            var rowKeys = cut.FindAll("tbody tr").Select(r => r.GetAttribute("data-id")!).ToList();
            rowKeys.Reverse();

            await cut.InvokeAsync(() => cut.Instance.UpdateSelectorTemplateOrder(rowKeys));

            var rows = cut.FindAll("tbody tr");
            var templateCodes = rows.Select(r => r.QuerySelectorAll("td")[2].TextContent.Trim()).ToList();
            Assert.Equal(new[] { "HDP", "O/U", "ML" }, templateCodes);

            // Both sort orders renumbered by the new visual order within each sport:
            // sport 2 (HDP) -> 1; sport 1 (O/U, ML) -> 1, 2.
            Assert.Equal(new[] { "1", "1", "2" }, AsiaSortOrders(rows));
            Assert.Equal(new[] { "1", "1", "2" }, EuroSortOrders(rows));
        }

        [Fact]
        public void DeletingARowRenumbersTheRemainingSortOrders()
        {
            // Three rows in one sport so removing the middle one leaves a gap unless renumbered.
            _backofficeService
                .Setup(s => s.GetClientSelectorMarketTemplates(ClientId))
                .ReturnsAsync(new BaseResponse<List<ClientSelectorMarketTemplateModel>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<ClientSelectorMarketTemplateModel>
                    {
                        new ClientSelectorMarketTemplateModel { SportId = 1, MarketTemplateId = "1001", MarketTemplateCode = "ML", AsiaSortOrder = 1, EuroSortOrder = 7, Identifier = "FT" },
                        new ClientSelectorMarketTemplateModel { SportId = 1, MarketTemplateId = "1002", MarketTemplateCode = "O/U", AsiaSortOrder = 2, EuroSortOrder = 8, Identifier = "FT" },
                        new ClientSelectorMarketTemplateModel { SportId = 1, MarketTemplateId = "1003", MarketTemplateCode = "HTFT", AsiaSortOrder = 3, EuroSortOrder = 9, Identifier = "FT" }
                    }
                });

            var cut = RenderPageWithClientSelected();

            cut.FindAll("tbody .link-danger")[1].Click();

            var rows = cut.FindAll("tbody tr");
            Assert.Equal(2, rows.Count);

            var templateCodes = rows.Select(r => r.QuerySelectorAll("td")[2].TextContent.Trim()).ToList();
            Assert.Equal(new[] { "ML", "HTFT" }, templateCodes);

            // No gap left behind where O/U was.
            Assert.Equal(new[] { "1", "2" }, AsiaSortOrders(rows));
            Assert.Equal(new[] { "1", "2" }, EuroSortOrders(rows));
        }

        [Fact]
        public void SavedValuesMissingFromLookupsRenderFallbackOptions()
        {
            _backofficeService
                .Setup(s => s.GetClientSelectorMarketTemplates(ClientId))
                .ReturnsAsync(new BaseResponse<List<ClientSelectorMarketTemplateModel>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<ClientSelectorMarketTemplateModel>
                    {
                        new ClientSelectorMarketTemplateModel { SportId = 99, MarketTemplateId = "9999", MarketTemplateCode = "XX", AsiaSortOrder = 1, EuroSortOrder = 1, Identifier = "HT" }
                    }
                });

            var cut = RenderPageWithClientSelected();

            var row = cut.FindAll("tbody tr")[0];

            var sportOptions = row.QuerySelectorAll("select")[0].QuerySelectorAll("option");
            var sportFallback = sportOptions.FirstOrDefault(o => o.GetAttribute("value") == "99");
            Assert.NotNull(sportFallback);
            Assert.Contains("missing from lookup", sportFallback!.TextContent);

            var templateOptions = row.QuerySelectorAll("select")[1].QuerySelectorAll("option");
            var templateFallback = templateOptions.FirstOrDefault(o => o.GetAttribute("value") == "9999");
            Assert.NotNull(templateFallback);
            Assert.Contains("XX (9999)", templateFallback!.TextContent);
        }

        [Fact]
        public void TemplateDropdownOnlyOffersTemplatesForTheRowSport()
        {
            var cut = RenderPageWithClientSelected();

            var rows = cut.FindAll("tbody tr");

            // Sport 1 rows: placeholder + templates 1001 and 1002.
            var sportOneOptions = rows[0].QuerySelectorAll("select")[1].QuerySelectorAll("option")
                .Select(o => o.GetAttribute("value")).ToList();
            Assert.Equal(new[] { "", "1001", "1002" }, sportOneOptions);

            // Sport 2 row: placeholder + template 2001 only.
            var sportTwoOptions = rows[2].QuerySelectorAll("select")[1].QuerySelectorAll("option")
                .Select(o => o.GetAttribute("value")).ToList();
            Assert.Equal(new[] { "", "2001" }, sportTwoOptions);
        }
    }
}
