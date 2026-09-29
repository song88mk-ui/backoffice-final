using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;
using MaintenanceV2Page = Backoffice.Web.Pages.Administration.MaintenanceV2;

namespace Backoffice.Web.Tests.Administration
{
    // BO-115: checking a client that isn't already under maintenance, in "Specific Clients"
    // mode, must actually enable maintenance for it on Save. The master "Enable Maintenance
    // Mode" switch and the per-client checklist used to be independent state, so Save could
    // silently disable (or no-op) instead of enable, with no error and a misleading toast.
    public class MaintenanceV2PageTests : TestContext
    {
        private static readonly Guid ClientGuid = Guid.Parse("AAAAAAAA-2222-3333-4444-555555555555");

        private readonly Mock<IBackofficeService> _backofficeService = new();

        public MaintenanceV2PageTests()
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
            userState.Model.Claims.Add(new Claim("Admin.Maintenance", $"{(int)Access.Edit}"));

            var translationState = new TranslationState(translationService.Object);

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

            _backofficeService
                .Setup(s => s.AddActivityLog(It.IsAny<ActivityLogModel>()))
                .ReturnsAsync(new BaseResponse<MessageResponse>() { StatusCode = StatusCode.Success });
        }

        private void SeedClient(bool isMaintenance)
        {
            _backofficeService
                .Setup(s => s.GetMaintenanceV2())
                .ReturnsAsync(new BaseResponse<MaintenanceResponse>()
                {
                    StatusCode = StatusCode.Success,
                    Result = new MaintenanceResponse()
                    {
                        ClientConfig = new List<MaintenanceConfigModel>()
                        {
                            new MaintenanceConfigModel()
                            {
                                App = "AL",
                                ClientId = ClientGuid.ToString(),
                                ClientName = "Acme Sports",
                                ClientCode = "ACME",
                                IsMaintenance = isMaintenance
                            }
                        }
                    }
                });
        }

        [Fact]
        public void CheckingALiveClient_ThenSaving_SendsIsMaintenanceTrue()
        {
            SeedClient(isMaintenance: false);

            SetMaintenanceRequest? sentRequest = null;
            _backofficeService
                .Setup(s => s.SetMaintenanceV2(It.IsAny<SetMaintenanceRequest>()))
                .Callback<SetMaintenanceRequest>(r => sentRequest = r)
                .ReturnsAsync(new BaseResponse<MaintenanceResponse>()
                {
                    StatusCode = StatusCode.Success,
                    Result = new MaintenanceResponse() { ClientConfig = new List<MaintenanceConfigModel>() }
                });

            var cut = RenderComponent<MaintenanceV2Page>();

            cut.Find("#radio2").Change(true);
            cut.Find("#client-dropdown").Click();
            cut.Find("li .form-check").Click();
            cut.Find("button.btn-save").Click();

            cut.WaitForAssertion(() => Assert.NotNull(sentRequest));
            Assert.True(sentRequest!.IsMaintenance);
        }

        [Fact]
        public void DisablingAnAlreadyMaintainedClient_StillSendsIsMaintenanceFalse()
        {
            SeedClient(isMaintenance: true);

            SetMaintenanceRequest? sentRequest = null;
            _backofficeService
                .Setup(s => s.SetMaintenanceV2(It.IsAny<SetMaintenanceRequest>()))
                .Callback<SetMaintenanceRequest>(r => sentRequest = r)
                .ReturnsAsync(new BaseResponse<MaintenanceResponse>()
                {
                    StatusCode = StatusCode.Success,
                    Result = new MaintenanceResponse() { ClientConfig = new List<MaintenanceConfigModel>() }
                });

            var cut = RenderComponent<MaintenanceV2Page>();

            cut.Find("#radio2").Change(true);
            // Client is already checked (loaded as under maintenance); turn the master
            // switch off to disable it without touching the checkbox.
            cut.Find("#maitenance-mode").Change(false);
            cut.Find("button.btn-save").Click();

            cut.WaitForAssertion(() => Assert.NotNull(sentRequest));
            Assert.False(sentRequest!.IsMaintenance);
        }
    }
}
