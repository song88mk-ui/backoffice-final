using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Administration.ClientSiteConfig;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text.Json;
using Xunit;

namespace Backoffice.Web.Tests.Administration
{
    // Tests for the Client Site Config activity log modal (GM-700): the From/To JSON diff
    // extraction and the rendered table. Every component in Backoffice.Web receives all
    // _Imports.razor @inject services, so the fixture registers the full set.
    public class ClientSiteConfigLogsModalTests : TestContext
    {
        private static readonly Guid ClientGuid = Guid.Parse("11111111-2222-3333-4444-555555555555");

        private readonly Mock<IBackofficeService> _backofficeService = new();

        public ClientSiteConfigLogsModalTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;

            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            Services.AddSingleton<IConfiguration>(configuration);
            Services.AddSingleton(new Mock<IModalService>().Object);
            Services.AddSingleton(new Mock<IPreRenderService>().Object);
            Services.AddSingleton(new Mock<ITradingService>().Object);
            Services.AddSingleton(new Mock<IProviderMappingService>().Object);
            Services.AddSingleton(_backofficeService.Object);
            Services.AddSingleton(new Mock<IAuthenticationService>().Object);
            Services.AddSingleton(new Mock<ITableauService>().Object);
            Services.AddSingleton(new Mock<ICommonService>().Object);
            Services.AddSingleton(new TranslationState(translationService.Object));
            Services.AddSingleton<UserState>();
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                new Mock<IAuthenticationService>().Object,
                sp.GetRequiredService<NavigationManager>()));
        }

        private static string ConfigJson(bool darkMode = false, List<string>? productNames = null, List<string>? alThemes = null)
        {
            var config = new ClientFeatureFlagsResponse()
            {
                ClientCode = ClientGuid.ToString().ToUpper(),
                Features = new ClientFeatureFlags()
                {
                    DarkMode = darkMode,
                    GameTrackerProductNames = productNames ?? new List<string>()
                }
            };

            if (alThemes != null)
                config.Layout["AL"] = new ClientLayoutTheme() { Themes = alThemes };

            return JsonSerializer.Serialize(config);
        }

        private IRenderedComponent<ClientSiteConfigLogsModal> Render(List<ActivityLogModel> logs)
        {
            _backofficeService
                .Setup(s => s.GetActivityLogs(It.IsAny<ActivityLogRequest>()))
                .ReturnsAsync(new BaseResponse<ActivityLogResponse>()
                {
                    StatusCode = StatusCode.Success,
                    Result = new ActivityLogResponse()
                    {
                        ActivityLogs = logs,
                        MetaData = new MetaData() { CurrentPage = 1, PageSize = 10, TotalCount = logs.Count, TotalPages = 1 }
                    }
                });

            var parameters = new ModalParameters();
            parameters.Add("Clients", new List<PlatformClientResponse>()
            {
                new PlatformClientResponse() { ClientId = ClientGuid, ClientName = "Acme Sports", ClientCode = "ACME" }
            });

            return RenderComponent<ClientSiteConfigLogsModal>(ps => ps.AddCascadingValue(parameters));
        }

        [Fact]
        public void GetChanges_FlagChange_ReturnsFriendlyLabelAndValues()
        {
            var changes = ClientSiteConfigLogsModal.GetChanges(ConfigJson(darkMode: false), ConfigJson(darkMode: true));

            Assert.NotNull(changes);
            var change = Assert.Single(changes!);
            Assert.Equal("Dark Mode", change.Setting);
            Assert.Equal("False", change.From);
            Assert.Equal("True", change.To);
        }

        [Fact]
        public void GetChanges_ProductNamesAndThemes_Detected()
        {
            var from = ConfigJson(productNames: new List<string>() { "song88-light" }, alThemes: new List<string>() { "red" });
            var to = ConfigJson(productNames: new List<string>() { "song88-light", "song88-dark" }, alThemes: new List<string>() { "red", "blue" });

            var changes = ClientSiteConfigLogsModal.GetChanges(from, to);

            Assert.NotNull(changes);
            Assert.Equal(2, changes!.Count);

            var productChange = changes.Single(c => c.Setting == "Game Tracker Product Names");
            Assert.Equal("song88-light", productChange.From);
            Assert.Equal("song88-light, song88-dark", productChange.To);

            var themeChange = changes.Single(c => c.Setting == "Themes (AL)");
            Assert.Equal("red", themeChange.From);
            Assert.Equal("red, blue", themeChange.To);
        }

        [Fact]
        public void GetChanges_NoChanges_ReturnsEmpty()
        {
            var changes = ClientSiteConfigLogsModal.GetChanges(ConfigJson(darkMode: true), ConfigJson(darkMode: true));

            Assert.NotNull(changes);
            Assert.Empty(changes!);
        }

        [Fact]
        public void GetChanges_InvalidJson_ReturnsNull()
        {
            Assert.Null(ClientSiteConfigLogsModal.GetChanges("not json", ConfigJson()));
            Assert.Null(ClientSiteConfigLogsModal.GetChanges("null", ConfigJson()));
        }

        [Fact]
        public void Render_ShowsLogRow_WithClientNameAndDiff()
        {
            var cut = Render(new List<ActivityLogModel>()
            {
                new ActivityLogModel()
                {
                    Page = "Admin.ClientSiteConfig",
                    Activity = "Update",
                    From = ConfigJson(darkMode: false),
                    To = ConfigJson(darkMode: true),
                    Payload = $"jane.doe.{ClientGuid.ToString().ToUpper()}",
                    UserName = "jane.doe",
                    TimeStamp = DateTime.UtcNow
                }
            });

            Assert.Contains("Acme Sports (ACME)", cut.Markup);
            Assert.Contains("Update", cut.Markup);
            Assert.Contains("Dark Mode", cut.Markup);
            Assert.Contains("jane.doe", cut.Markup);
        }

        [Fact]
        public void Render_NoLogs_ShowsNoRecordFound()
        {
            var cut = Render(new List<ActivityLogModel>());

            Assert.Contains("No Record Found", cut.Markup);
        }
    }
}
