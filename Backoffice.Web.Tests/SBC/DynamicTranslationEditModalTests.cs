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
    // Component tests for the Dynamic Translation (glossary) edit modal, covering the String ID
    // field's disabled-on-edit behavior (StringId is the CSV import/export upsert key and the
    // repository already ignores a changed value on edit -- the UI must not suggest otherwise).
    public class DynamicTranslationEditModalTests : BunitTestContext
    {
        private readonly Mock<IModalService> _modalService = new();

        public DynamicTranslationEditModalTests()
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

        private IRenderedComponent<DynamicTranslationEditModal> Render(
            bool isNew,
            string stringId = "SpId:1",
            string sourceText = "Soccer",
            string targetText = "",
            string selectedLanguageCode = "ZH",
            Func<string, string, string, Task<bool>>? onSave = null)
        {
            var parameters = new ModalParameters();
            parameters.Add("StringId", stringId);
            parameters.Add("SourceText", sourceText);
            parameters.Add("TargetText", targetText);
            parameters.Add("SelectedLanguageCode", selectedLanguageCode);
            parameters.Add("IsNew", isNew);
            parameters.Add("OnSaveCallback", onSave ?? ((id, src, tgt) => Task.FromResult(true)));

            return Render<DynamicTranslationEditModal>(ps => ps.AddCascadingValue(parameters));
        }

        [Fact]
        public void EditingExistingGlossaryTerm_StringIdInputIsDisabled_WithHelperNote()
        {
            var cut = Render(isNew: false);

            var input = cut.Find("input.form-control");
            Assert.True(input.HasAttribute("disabled"));
            Assert.Equal("SpId:1", input.GetAttribute("value"));
            Assert.Contains("cannot be changed", cut.Markup);
        }

        [Fact]
        public void AddingNewGlossaryTerm_StringIdInputIsEditable_WithoutHelperNote()
        {
            var cut = Render(isNew: true, stringId: string.Empty, sourceText: string.Empty);

            var input = cut.Find("input.form-control");
            Assert.False(input.HasAttribute("disabled"));
            Assert.DoesNotContain("cannot be changed", cut.Markup);
        }

        [Fact]
        public void Save_InvokesCallback_WithStringIdUnchanged_AndClosesModal_OnSuccess()
        {
            string? savedStringId = null;
            var cut = Render(isNew: false, targetText: "", onSave: (id, src, tgt) =>
            {
                savedStringId = id;
                return Task.FromResult(true);
            });

            cut.FindAll("textarea.form-control")[1].Input("足球"); // translation textarea (ZH shown, EN excluded)
            cut.Find("button.btn-primary").Click();

            Assert.Equal("SpId:1", savedStringId);
            _modalService.Verify(m => m.Close(), Times.Once);
        }

        [Fact]
        public void EnglishLanguage_DoesNotShowTranslationField()
        {
            var cut = Render(isNew: false, selectedLanguageCode: "EN");

            var textareas = cut.FindAll("textarea.form-control");
            Assert.Single(textareas); // only Source Text (EN); no separate translation field
        }
    }
}
