using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.OddsEngine.Configuration;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Config;
using Backoffice.Web.Pages.OddsEngine;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Backoffice.Web.Tests.OddsEngine
{
    // BO-143: the OE Config page showed "Calculating..." forever, or an empty list, when a call failed, and a real
    // count of 0 also read "Calculating...". A failure is now shown as an error, and the loading, zero and failed
    // states of a tab count are told apart.
    public class ConfigLoadFailureTests : BunitTestContext
    {
        private const string ConfigAlert = "Some of the Config information could not be loaded";
        private const string LeaguesAlert = "The leagues could not be loaded";
        private const string TemplatesAlert = "The templates could not be loaded";

        private readonly Mock<IBackofficeService> _backofficeService = new();
        private readonly Mock<ITradingService> _tradingService = new();

        public ConfigLoadFailureTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;

            // Every component under Backoffice.Web picks up the @inject list in _Imports.razor, so bUnit needs them all.
            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());

            var authenticationService = new Mock<IAuthenticationService>();
            authenticationService
                .Setup(s => s.AuthenticateToken(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<TokenModel> { StatusCode = StatusCode.Success });

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            var translationState = new TranslationState(translationService.Object);

            Services.AddSingleton<IConfiguration>(configuration);
            Services.AddSingleton<IModalService>(new ModalService(translationState));
            Services.AddSingleton(new Mock<IPreRenderService>().Object);
            Services.AddSingleton(_tradingService.Object);
            Services.AddSingleton(new Mock<IProviderMappingService>().Object);
            Services.AddSingleton(_backofficeService.Object);
            Services.AddSingleton(authenticationService.Object);
            Services.AddSingleton(new Mock<ITableauService>().Object);
            Services.AddSingleton(new Mock<ICommonService>().Object);
            Services.AddSingleton(translationState);
            Services.AddSingleton(new UserState());
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                authenticationService.Object,
                sp.GetRequiredService<NavigationManager>()));

            var authContext = this.AddAuthorization();
            authContext.SetAuthorized("test-user");
            authContext.SetPolicies("sbc-config", "sbc-config-canedit");

            _backofficeService
                .Setup(s => s.GetSportLookup(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<SportsLookupResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new SportsLookupResponse { SportLookup = new List<SportLookupModel> { new() { SportId = 1, SportName = "Soccer" } } }
                });
        }

        private static BaseResponse<T> Failed<T>(string message = "Error (Bad Request)") => new() { StatusCode = StatusCode.Error, Message = message };

        private static BaseResponse<ConfigTemplateLeagueResponse> NoLeagues() => new()
        {
            StatusCode = StatusCode.Success,
            Result = new ConfigTemplateLeagueResponse { ClientConfigs = new List<ConfigClientModel>(), MetaData = new MetaData() }
        };

        private static ConfigContentFilterModel Filter() => new()
        {
            Clients = new List<SelectListItemWithCheckboxModel<string>>(),
            Sports = new List<SelectListItemModel<string>> { new() { Value = "1", Name = "Soccer" } },
            Scopes = new List<SelectListItemModel<string>> { new() { Name = "Live", Value = "0" }, new() { Name = "Prematch", Value = "1" } }
        };

        private IRenderedComponent<ConfigNavMenu> RenderNavMenu(int myTemplates, int assigned, int unAssigned) =>
            Render<ConfigNavMenu>(ps => ps.Add(p => p.ParamModel, new ConfigNavMenuParamModel
            {
                MyTemplatesCount = myTemplates,
                AssignedLeaguesCount = assigned,
                UnAssignedLeaguesCount = unAssigned
            }));

        private static List<string> Tabs(IRenderedComponent<ConfigNavMenu> cut) =>
            cut.FindAll("a.config-nav").Select(a => a.TextContent.Trim()).ToList();

        // ---- The tab labels ----------------------------------------------------------------------------------

        [Fact]
        public void ACountThatIsStillLoading_ShowsCalculating()
        {
            var cut = RenderNavMenu(ConfigNavMenuParamModel.CountLoading, ConfigNavMenuParamModel.CountLoading, ConfigNavMenuParamModel.CountLoading);

            Assert.Equal(new[] { "My Templates (Calculating...)", "Assigned (Calculating...)", "UnAssigned (Calculating...)" }, Tabs(cut));
        }

        [Fact]
        public void ARealCountOfZero_ShowsZero_NotCalculating()
        {
            var cut = RenderNavMenu(4, 0, 0);

            Assert.Equal(new[] { "My Templates (4)", "Assigned (0)", "UnAssigned (0)" }, Tabs(cut));
        }

        [Fact]
        public void ACountThatFailedToLoad_ShowsError()
        {
            var cut = RenderNavMenu(ConfigNavMenuParamModel.CountFailed, 48, ConfigNavMenuParamModel.CountFailed);

            Assert.Equal(new[] { "My Templates (Error)", "Assigned (48)", "UnAssigned (Error)" }, Tabs(cut));
        }

        [Fact]
        public void ANewParamModel_StartsInTheLoadingState()
        {
            var model = new ConfigNavMenuParamModel();

            Assert.Equal(ConfigNavMenuParamModel.CountLoading, model.MyTemplatesCount);
            Assert.Equal(ConfigNavMenuParamModel.CountLoading, model.AssignedLeaguesCount);
            Assert.Equal(ConfigNavMenuParamModel.CountLoading, model.UnAssignedLeaguesCount);
        }

        // ---- The leagues list (Assigned and Unassigned tabs) -------------------------------------------------

        private IRenderedComponent<ConfigAssigned> RenderLeagues(ConfigState state) =>
            Render<ConfigAssigned>(ps => ps.Add(p => p.State, state).Add(p => p.FilterModel, Filter()));

        [Fact]
        public void AFailedLeaguesLoad_ShowsAnErrorMessage_NotJustNoRecords()
        {
            _backofficeService
                .Setup(s => s.GetConfigClient(It.IsAny<ConfigState>(), It.IsAny<ConfigTemplateLeagueRequest>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Failed<ConfigTemplateLeagueResponse>("Invalid object name 'ConfigTemplateAssigned'."));

            var cut = RenderLeagues(ConfigState.Assigned);

            cut.WaitForAssertion(() => Assert.Contains(LeaguesAlert, cut.Markup));
            Assert.Contains("alert-danger", cut.Markup);
            Assert.DoesNotContain("ConfigTemplateAssigned", cut.Markup); // the API's text goes to the log, not the screen
        }

        [Fact]
        public void ALaterSuccessfulLeaguesLoad_RemovesTheErrorMessage()
        {
            _backofficeService
                .SetupSequence(s => s.GetConfigClient(It.IsAny<ConfigState>(), It.IsAny<ConfigTemplateLeagueRequest>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Failed<ConfigTemplateLeagueResponse>())
                .ReturnsAsync(NoLeagues());

            var cut = RenderLeagues(ConfigState.Assigned);
            cut.WaitForAssertion(() => Assert.Contains(LeaguesAlert, cut.Markup));

            cut.Render(ps => ps.Add(p => p.State, ConfigState.UnAssigned));

            cut.WaitForAssertion(() => Assert.DoesNotContain(LeaguesAlert, cut.Markup));
        }

        [Fact]
        public void ASuccessfulLeaguesLoad_ShowsNoErrorMessage()
        {
            _backofficeService
                .Setup(s => s.GetConfigClient(It.IsAny<ConfigState>(), It.IsAny<ConfigTemplateLeagueRequest>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(NoLeagues());

            var cut = RenderLeagues(ConfigState.Assigned);

            cut.WaitForAssertion(() => Assert.Contains("No Record Found", cut.Markup));
            Assert.DoesNotContain(LeaguesAlert, cut.Markup);
        }

        // ---- My Templates ------------------------------------------------------------------------------------

        [Fact]
        public void AFailedTemplatesLoad_ShowsAnErrorMessage()
        {
            _backofficeService
                .Setup(s => s.GetConfigTemplate(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Failed<ConfigTemplateResponse>("Error: Column, parameter, or variable @SportIds. : Cannot find data type dbo.IntList."));

            var cut = Render<ConfigMyTemplates>(ps => ps.Add(p => p.FilterModel, Filter()));

            cut.WaitForAssertion(() => Assert.Contains(TemplatesAlert, cut.Markup));
            Assert.DoesNotContain("IntList", cut.Markup);
        }

        // ---- The whole page ----------------------------------------------------------------------------------

        [Fact]
        public void WhenTheCountsAndTheClientListFail_ThePageSaysSo_InsteadOfCalculatingForever()
        {
            // Configuration2.Init() clears its preloader without a render of its own, so the page relies on a count
            // call finishing after the sports and client calls, as it does over HTTP. Delay the first count to match.
            _backofficeService
                .Setup(s => s.GetTemplateConfigCount(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Failed<int>());
            _backofficeService
                .Setup(s => s.GetTemplateConfigCount(string.Empty, It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await Task.Delay(150);
                    return Failed<int>();
                });
            _tradingService
                .Setup(t => t.GetClientList(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Failed<List<ClientInformationModel>>());
            _backofficeService
                .Setup(s => s.GetConfigTemplate(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Failed<ConfigTemplateResponse>());

            var cut = Render<Configuration2>();

            cut.WaitForAssertion(() =>
            {
                Assert.Contains(ConfigAlert, cut.Markup);
                var tabs = cut.FindAll("a.config-nav").Select(a => a.TextContent.Trim()).ToList();
                Assert.Equal(new[] { "My Templates (Error)", "Assigned (Error)", "UnAssigned (Error)" }, tabs);
            }, TimeSpan.FromSeconds(5));
            Assert.DoesNotContain("Calculating", cut.Markup);
        }

        // ---- The banner after a template Save ----------------------------------------------------------------

        [Fact]
        public void TheTextInTheMParameter_IsShownOnMyTemplates_AsTheBannerAfterASave()
        {
            // The template editor redirects here with ?M=... after a Save, and this page reads only the parameter "M".
            Services.GetRequiredService<NavigationManager>()
                .NavigateTo("http://localhost/hk/odds-engine/config-templates?M=Successfully%20updated");

            _backofficeService
                .Setup(s => s.GetTemplateConfigCount(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BaseResponse<int> { StatusCode = StatusCode.Success, Result = 0 });
            // Configuration2.Init() clears its preloader without a render of its own, so delay the first count as above.
            _backofficeService
                .Setup(s => s.GetTemplateConfigCount(string.Empty, It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await Task.Delay(150);
                    return new BaseResponse<int> { StatusCode = StatusCode.Success, Result = 0 };
                });
            _tradingService
                .Setup(t => t.GetClientList(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BaseResponse<List<ClientInformationModel>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<ClientInformationModel> { new() { ClientCode = "AAA", ClientName = "Client A" } }
                });
            _backofficeService
                .Setup(s => s.GetConfigTemplate(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BaseResponse<ConfigTemplateResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ConfigTemplateResponse { ConfigTemplates = new List<ConfigTemplateModel>() }
                });

            var cut = Render<Configuration2>();

            cut.WaitForAssertion(() => Assert.Contains("Successfully updated", cut.Markup), TimeSpan.FromSeconds(5));
        }
    }
}
