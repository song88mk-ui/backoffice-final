using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;
using ClientSiteConfigPage = Backoffice.Web.Pages.Administration.ClientSiteConfig;

namespace Backoffice.Web.Tests.Administration
{
    // Page tests for Client Site Config, covering the GM-697 QA finding: a client without an
    // existing config (or with a config that has no Layout data) must still preload the full
    // form, including the Themes section with the known platform-web layouts (AL, EL2).
    public class ClientSiteConfigPageTests : TestContext
    {
        private static readonly Guid ClientGuid = Guid.Parse("11111111-2222-3333-4444-555555555555");

        private readonly Mock<IBackofficeService> _backofficeService = new();

        public ClientSiteConfigPageTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;

            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());

            var authenticationService = new Mock<IAuthenticationService>();
            authenticationService
                .Setup(s => s.AuthenticateToken(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<TokenModel>() { StatusCode = StatusCode.Success });

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            var userState = new UserState();
            userState.Model.Claims.Add(new Claim("Admin.ClientSiteConfig", $"{(int)Access.Edit}"));

            var translationState = new TranslationState(translationService.Object);

            Services.AddSingleton<IConfiguration>(configuration);
            // CustomModal (rendered by the page) casts IModalService to the concrete
            // ModalService in OnInitialized, so a Moq proxy is not usable here.
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

            _backofficeService
                .Setup(s => s.GetPlatformClients())
                .ReturnsAsync(new BaseResponse<PlatformClientModel>()
                {
                    StatusCode = StatusCode.Success,
                    Result = new PlatformClientModel()
                    {
                        Clients = new List<PlatformClientResponse>()
                        {
                            new PlatformClientResponse() { ClientId = ClientGuid, ClientName = "Acme Sports", ClientCode = "ACME" }
                        }
                    }
                });
        }

        private IRenderedComponent<ClientSiteConfigPage> RenderWithConfigResponse(BaseResponse<ClientFeatureFlagsResponse> response)
        {
            _backofficeService
                .Setup(s => s.GetClientFeatureFlags(It.IsAny<string>()))
                .ReturnsAsync(response);

            var cut = RenderComponent<ClientSiteConfigPage>();
            cut.Find("select").Change(ClientGuid.ToString().ToUpper());
            return cut;
        }

        [Fact]
        public void NoConfigClient_PreloadsFullForm_IncludingThemes()
        {
            var cut = RenderWithConfigResponse(new BaseResponse<ClientFeatureFlagsResponse>()
            {
                StatusCode = StatusCode.NotFound
            });

            Assert.Contains("No existing configuration found", cut.Markup);
            Assert.Equal(8, cut.FindAll(".feature-flag").Count);
            Assert.Contains("Themes", cut.Markup);
            Assert.NotNull(cut.Find("#theme-AL-red"));
            Assert.NotNull(cut.Find("#theme-EL2-white"));
        }

        [Fact]
        public void ConfigWithoutLayout_StillShowsThemeCheckboxes()
        {
            var cut = RenderWithConfigResponse(new BaseResponse<ClientFeatureFlagsResponse>()
            {
                StatusCode = StatusCode.Success,
                Result = new ClientFeatureFlagsResponse() { ClientCode = ClientGuid.ToString() }
            });

            Assert.NotNull(cut.Find("#theme-AL-red"));
            Assert.NotNull(cut.Find("#theme-EL2-red"));
        }

        [Fact]
        public void TogglingThemeOnNewConfig_CreatesLayoutEntry_AndEnablesSave()
        {
            var cut = RenderWithConfigResponse(new BaseResponse<ClientFeatureFlagsResponse>()
            {
                StatusCode = StatusCode.NotFound
            });

            // The Game Tracker "Add" button shares the btn-save class, so match the Save button by text.
            Assert.DoesNotContain(cut.FindAll("button.btn-save"), b => b.TextContent.Trim() == "Save");

            cut.Find("#theme-AL-red").Change(true);

            Assert.True(cut.Find("#theme-AL-red").HasAttribute("checked"));
            Assert.Contains(cut.FindAll("button.btn-save"), b => b.TextContent.Trim() == "Save");
        }

        [Fact]
        public void ExistingLayout_ChecksStoredThemes()
        {
            var config = new ClientFeatureFlagsResponse() { ClientCode = ClientGuid.ToString() };
            config.Layout["AL"] = new ClientLayoutTheme() { Themes = new List<string>() { "red", "blue" } };

            var cut = RenderWithConfigResponse(new BaseResponse<ClientFeatureFlagsResponse>()
            {
                StatusCode = StatusCode.Success,
                Result = config
            });

            Assert.True(cut.Find("#theme-AL-red").HasAttribute("checked"));
            Assert.True(cut.Find("#theme-AL-blue").HasAttribute("checked"));
            Assert.False(cut.Find("#theme-AL-gray").HasAttribute("checked"));
            Assert.NotNull(cut.Find("#theme-EL2-red"));
        }

        [Fact]
        public void UnknownStoredLayout_RendersReadOnly_AlongsideKnownLayouts()
        {
            var config = new ClientFeatureFlagsResponse() { ClientCode = ClientGuid.ToString() };
            config.Layout["XL9"] = new ClientLayoutTheme() { Themes = new List<string>() { "neon" } };

            var cut = RenderWithConfigResponse(new BaseResponse<ClientFeatureFlagsResponse>()
            {
                StatusCode = StatusCode.Success,
                Result = config
            });

            Assert.Contains("Unknown layout - themes managed elsewhere: neon", cut.Markup);
            Assert.NotNull(cut.Find("#theme-AL-red"));
            Assert.NotNull(cut.Find("#theme-EL2-red"));
        }
    }
}
