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
    // Component tests for the Static Translation edit modal: the Translation Key field's
    // disabled-on-edit behavior (the backing DB row is looked up by LanguageCode+Name, so
    // changing the key on an existing row must not be possible from the UI), and the
    // Source Text (EN) / Translation split introduced when the page was restructured to
    // mirror Dynamic Translation.
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
            bool isNew,
            string key = "1st_half",
            string sourceText = "1st Half",
            string targetText = "前半",
            Func<string, string, string, Task<bool>>? onSave = null)
        {
            var parameters = new ModalParameters();
            parameters.Add("TranslationKey", key);
            parameters.Add("SourceText", sourceText);
            parameters.Add("TargetText", targetText);
            parameters.Add("SelectedLanguageCode", "ZH");
            parameters.Add("IsNew", isNew);
            parameters.Add("OnSaveCallback", onSave ?? ((k, s, t) => Task.FromResult(true)));

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
            var cut = Render(isNew: true, key: string.Empty, sourceText: string.Empty, targetText: string.Empty);

            var input = cut.Find("input.form-control");
            Assert.False(input.HasAttribute("disabled"));
            Assert.DoesNotContain("cannot be changed", cut.Markup);
        }

        [Fact]
        public void Render_ShowsSourceAndTranslationFields_WithLanguageLabel()
        {
            var cut = Render(isNew: false);

            var textareas = cut.FindAll("textarea.form-control");
            Assert.Equal(2, textareas.Count);
            Assert.Contains("Source Text (EN)", cut.Markup);
            Assert.Contains("Translation (ZH)", cut.Markup);
        }

        [Fact]
        public void Save_InvokesCallbackWithKeySourceAndTarget_AndClosesModal_OnSuccess()
        {
            string? savedKey = null;
            string? savedSource = null;
            string? savedTarget = null;
            var cut = Render(isNew: false, onSave: (k, s, t) =>
            {
                savedKey = k;
                savedSource = s;
                savedTarget = t;
                return Task.FromResult(true);
            });

            // Re-query between inputs: each Input() re-renders, invalidating cached elements.
            cut.FindAll("textarea.form-control")[0].Input("Updated Source");
            cut.FindAll("textarea.form-control")[1].Input("Updated Translation");
            cut.Find("button.btn-primary").Click();

            Assert.Equal("1st_half", savedKey);
            Assert.Equal("Updated Source", savedSource);
            Assert.Equal("Updated Translation", savedTarget);
            _modalService.Verify(m => m.Close(), Times.Once);
        }

        [Fact]
        public void Save_IsDisabled_WhenSourceTextIsBlank()
        {
            var cut = Render(isNew: false, sourceText: string.Empty);

            var saveButton = cut.Find("button.btn-primary");
            Assert.True(saveButton.HasAttribute("disabled"));
        }

        [Fact]
        public void Save_DoesNotCloseModal_WhenCallbackFails()
        {
            var cut = Render(isNew: false, onSave: (k, s, t) => Task.FromResult(false));

            cut.Find("button.btn-primary").Click();

            _modalService.Verify(m => m.Close(), Times.Never);
        }

        [Fact]
        public void Cancel_CancelsModal_WithoutInvokingCallback()
        {
            var invocations = 0;
            var cut = Render(isNew: false, onSave: (k, s, t) =>
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
