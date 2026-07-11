using Backoffice.Web.Models;
using Backoffice.Web.Pages.SBC;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Backoffice.Web.Tests.SBC
{
    // Component tests for the Static Translation edit modal, covering the Translation Key
    // field's disabled-on-edit behavior (the backing DB row is looked up by LanguageCode+Name,
    // so changing the key on an existing row must not be possible from the UI).
    public class StaticTranslationEditModalTests : TestContext
    {
        private readonly Mock<IModalService> _modalService = new();

        public StaticTranslationEditModalTests()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            Services.AddSingleton<IConfiguration>(configuration);
            Services.AddSingleton(_modalService.Object);
            Services.AddSingleton(new Mock<IPreRenderService>().Object);
            Services.AddSingleton(new Mock<ITradingService>().Object);
            Services.AddSingleton(new Mock<IProviderMappingService>().Object);
            Services.AddSingleton(new Mock<IBackofficeService>().Object);
            Services.AddSingleton(new Mock<IAuthenticationService>().Object);
            Services.AddSingleton(new Mock<ITableauService>().Object);
            Services.AddSingleton(new Mock<ICommonService>().Object);
            Services.AddSingleton(new TranslationState(new Mock<ITranslationService>().Object));
            Services.AddSingleton<UserState>();
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(sp => new PageState(
                new Mock<IAuthenticationService>().Object,
                sp.GetRequiredService<NavigationManager>()));
        }

        private IRenderedComponent<StaticTranslationEditModal> Render(
            bool isNew, string key = "1st_half", string value = "1st Half", Func<string, string, Task<bool>>? onSave = null)
        {
            var parameters = new ModalParameters();
            parameters.Add("TranslationKey", key);
            parameters.Add("TranslationValue", value);
            parameters.Add("IsNew", isNew);
            parameters.Add("OnSaveCallback", onSave ?? ((k, v) => Task.FromResult(true)));

            return RenderComponent<StaticTranslationEditModal>(ps => ps.AddCascadingValue(parameters));
        }

        [Fact]
        public void EditingExistingTranslation_KeyInputIsDisabled_WithHelperNote()
        {
            var cut = Render(isNew: false);

            var input = cut.Find("input.form-control");
            Assert.True(input.HasAttribute("disabled"));
            Assert.Equal("1st_half", input.GetAttribute("value"));
            Assert.Contains("cannot be changed", cut.Markup);
        }

        [Fact]
        public void AddingNewTranslation_KeyInputIsEditable_WithoutHelperNote()
        {
            var cut = Render(isNew: true, key: string.Empty, value: string.Empty);

            var input = cut.Find("input.form-control");
            Assert.False(input.HasAttribute("disabled"));
            Assert.DoesNotContain("cannot be changed", cut.Markup);
        }

        [Fact]
        public void Save_InvokesCallback_AndClosesModal_OnSuccess()
        {
            string? savedKey = null;
            string? savedValue = null;
            var cut = Render(isNew: false, onSave: (k, v) =>
            {
                savedKey = k;
                savedValue = v;
                return Task.FromResult(true);
            });

            cut.Find("textarea.form-control").Input("Updated Value");
            cut.Find("button.btn-primary").Click();

            Assert.Equal("1st_half", savedKey);
            Assert.Equal("Updated Value", savedValue);
            _modalService.Verify(m => m.Close(), Times.Once);
        }

        [Fact]
        public void Save_DoesNotCloseModal_WhenCallbackFails()
        {
            var cut = Render(isNew: false, onSave: (k, v) => Task.FromResult(false));

            cut.Find("button.btn-primary").Click();

            _modalService.Verify(m => m.Close(), Times.Never);
        }

        [Fact]
        public void Cancel_CancelsModal_WithoutInvokingCallback()
        {
            var invocations = 0;
            var cut = Render(isNew: false, onSave: (k, v) =>
            {
                invocations++;
                return Task.FromResult(true);
            });

            cut.Find("button.btn-secondary").Click();

            Assert.Equal(0, invocations);
            _modalService.Verify(m => m.Cancel(), Times.Once);
        }
    }
}
