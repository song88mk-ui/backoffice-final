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
    public class SelectorMarketTemplatesTests : TestContext
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

            var authContext = this.AddTestAuthorization();
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
            _backofficeService
                .Setup(s => s.GetClientSelectorMarketTemplates(ClientId))
                .ReturnsAsync(new BaseResponse<List<ClientSelectorMarketTemplateModel>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<ClientSelectorMarketTemplateModel>
                    {
                        new ClientSelectorMarketTemplateModel { SportId = 2, MarketTemplateId = "2001", MarketTemplateCode = "HDP", AsiaSortOrder = 1, EuroSortOrder = 1, Identifier = "FT" },
                        new ClientSelectorMarketTemplateModel { SportId = 1, MarketTemplateId = "1002", MarketTemplateCode = "O/U", AsiaSortOrder = 2, EuroSortOrder = 2, Identifier = "FT" },
                        new ClientSelectorMarketTemplateModel { SportId = 1, MarketTemplateId = "1001", MarketTemplateCode = "ML", AsiaSortOrder = 1, EuroSortOrder = 1, Identifier = "FT" }
                    }
                });
        }

        private IRenderedComponent<SelectorMarketTemplates> RenderPageWithClientSelected()
        {
            var cut = RenderComponent<SelectorMarketTemplates>();

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
