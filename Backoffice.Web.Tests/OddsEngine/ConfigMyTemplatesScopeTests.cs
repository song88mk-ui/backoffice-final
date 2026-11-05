using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
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
    // BO-151: My Templates opened on Live, so a template saved in Prematch showed "No Record Found" until the user found the
    // scope drop-down. It now opens on "All Scopes", which asks the API for no scope filter (scopeId -1).
    public class ConfigMyTemplatesScopeTests : BunitTestContext
    {
        private readonly Mock<IBackofficeService> _backofficeService = new();

        public ConfigMyTemplatesScopeTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;

            // Every component under Backoffice.Web picks up the @inject list in _Imports.razor, so bUnit needs them all.
            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());

            var authenticationService = new Mock<IAuthenticationService>();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

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
            Services.AddSingleton(new UserState());
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                authenticationService.Object,
                sp.GetRequiredService<NavigationManager>()));

            var authContext = this.AddAuthorization();
            authContext.SetAuthorized("test-user");
            authContext.SetPolicies("sbc-config", "sbc-config-canedit");
        }

        private static BaseResponse<ConfigTemplateResponse> Templates(params ConfigTemplateModel[] templates) => new()
        {
            StatusCode = StatusCode.Success,
            Result = new ConfigTemplateResponse { ConfigTemplates = templates.ToList() }
        };

        private static ConfigContentFilterModel Filter() => new()
        {
            Clients = new List<SelectListItemWithCheckboxModel<string>>(),
            Sports = new List<SelectListItemModel<string>> { new() { Value = "1", Name = "Soccer" } },
            Scopes = new List<SelectListItemModel<string>>
            {
                new() { Name = "Live", Value = ((int)Scope.Live).ToString() },
                new() { Name = "Prematch", Value = ((int)Scope.Prematch).ToString() }
            }
        };

        private IRenderedComponent<ConfigMyTemplates> RenderPage(BaseResponse<ConfigTemplateResponse> response)
        {
            _backofficeService
                .Setup(s => s.GetConfigTemplate(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            return Render<ConfigMyTemplates>(ps => ps.Add(p => p.FilterModel, Filter()));
        }

        private void VerifyLoadedWithScope(int scopeId) =>
            _backofficeService.Verify(
                s => s.GetConfigTemplate(It.IsAny<string>(), It.IsAny<int>(), scopeId, It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);

        // The sports drop-down is the first select, the scope drop-down the second.
        private static AngleSharp.Dom.IElement ScopeSelect(IRenderedComponent<ConfigMyTemplates> cut) => cut.FindAll("select")[1];

        [Fact]
        public void TheFirstLoad_AsksForEveryScope()
        {
            var cut = RenderPage(Templates());

            cut.WaitForAssertion(() => VerifyLoadedWithScope(-1));
            _backofficeService.Verify(
                s => s.GetConfigTemplate(It.IsAny<string>(), It.IsAny<int>(), It.Is<int>(scope => scope != -1), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public void TheScopeDropDown_ListsAllScopesFirst_AndItIsSelected()
        {
            var cut = RenderPage(Templates());

            var options = ScopeSelect(cut).QuerySelectorAll("option");

            Assert.Equal(new[] { "All Scopes", "Live", "Prematch" }, options.Select(o => o.TextContent.Trim()).ToArray());
            Assert.Equal(new[] { true, false, false }, options.Select(o => o.HasAttribute("selected")).ToArray());
        }

        [Fact]
        public void ChoosingAScope_AsksForThatScope_AndAllScopesAsksForEveryScopeAgain()
        {
            var cut = RenderPage(Templates());
            cut.WaitForAssertion(() => VerifyLoadedWithScope(-1));
            _backofficeService.Invocations.Clear();

            ScopeSelect(cut).Change(((int)Scope.Prematch).ToString());
            cut.WaitForAssertion(() => VerifyLoadedWithScope((int)Scope.Prematch));

            _backofficeService.Invocations.Clear();

            ScopeSelect(cut).Change("-1");
            cut.WaitForAssertion(() => VerifyLoadedWithScope(-1));
        }

        [Fact]
        public void ATemplateOfAnyScope_IsListed_NotNoRecordFound()
        {
            var cut = RenderPage(Templates(new ConfigTemplateModel
            {
                Id = 5,
                Name = "Dev Template Test-PreMatch",
                SportId = 1,
                FixtureStatusId = (int)Scope.Prematch,
                Timestamp = new DateTime(2026, 11, 2, 8, 22, 0)
            }));

            cut.WaitForAssertion(() => Assert.Contains("Dev Template Test-PreMatch", cut.Markup));
            Assert.DoesNotContain("No Record Found", cut.Markup);
        }

        // BO-136: the template export only forwarded a message to a queue nothing reads, so it produced no file.
        // The row no longer offers it; the other actions stay.
        [Fact]
        public void ATemplateRow_DoesNotOfferExport()
        {
            var cut = RenderPage(Templates(new ConfigTemplateModel
            {
                Id = 5,
                Name = "Dev Template Test-PreMatch",
                SportId = 1,
                FixtureStatusId = (int)Scope.Prematch,
                Timestamp = new DateTime(2026, 11, 2, 8, 22, 0)
            }));

            cut.WaitForAssertion(() => Assert.Contains("Dev Template Test-PreMatch", cut.Markup));
            Assert.Empty(cut.FindAll("a[title='Export Template']"));
            Assert.NotEmpty(cut.FindAll("a[title='Edit Template']"));
            Assert.NotEmpty(cut.FindAll("a[title='Delete Template']"));
        }
    }
}
