using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Administration.MaintenanceV2;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Backoffice.Web.Tests.Administration
{
    // The search box previously updated `_searchField` but never re-filtered the rendered
    // list or requested a re-render: the filter only ran inside OnParametersSet, which is
    // gated on the checked-item set changing, not on typed search text (BO-115 follow-up).
    public class ClientSelectionDropDownTests : BunitTestContext
    {
        public ClientSelectionDropDownTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;

            // ClientSelectionDropDown is under Backoffice.Web, so it picks up every @inject in
            // _Imports.razor even though it only uses _translationState directly; bUnit's DI
            // throws on render unless all of them are registered.
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

            var translationState = new TranslationState(translationService.Object);

            Services.AddSingleton<IConfiguration>(configuration);
            Services.AddSingleton<IModalService>(new ModalService(translationState));
            Services.AddSingleton(new Mock<IPreRenderService>().Object);
            Services.AddSingleton(new Mock<ITradingService>().Object);
            Services.AddSingleton(new Mock<IProviderMappingService>().Object);
            Services.AddSingleton(new Mock<IBackofficeService>().Object);
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
        }

        private static List<MaintenanceSelectListItemWithCheckboxModel> SampleClients() => new()
        {
            new() { Value = "AL-GIMLISTG", Name = "AL-GIMLISTG", IsChecked = false, IsMaintenanceEnabled = false },
            new() { Value = "EL1-QTE-Merema", Name = "EL1-QTE-Merema", IsChecked = false, IsMaintenanceEnabled = false },
            new() { Value = "EL1-QTE-Yeha", Name = "EL1-QTE-Yeha", IsChecked = false, IsMaintenanceEnabled = false },
        };

        [Fact]
        public void TypingInSearchBox_FiltersTheVisibleList()
        {
            var cut = Render<ClientSelectionDropDown>(parameters => parameters
                .Add(p => p.Clients, SampleClients()));

            cut.Find("#client-dropdown").Click();
            Assert.Equal(3, cut.FindAll("li[id]").Count);

            cut.Find("#search").Input("gimli");

            var visible = cut.FindAll("li[id]");
            Assert.Single(visible);
            Assert.Contains("AL-GIMLISTG", visible[0].TextContent);
        }

        [Fact]
        public void ClearingSearchBox_RestoresTheFullList()
        {
            var cut = Render<ClientSelectionDropDown>(parameters => parameters
                .Add(p => p.Clients, SampleClients()));

            cut.Find("#client-dropdown").Click();
            cut.Find("#search").Input("gimli");
            Assert.Single(cut.FindAll("li[id]"));

            cut.Find("#search").Input("");
            Assert.Equal(3, cut.FindAll("li[id]").Count);
        }
    }
}
