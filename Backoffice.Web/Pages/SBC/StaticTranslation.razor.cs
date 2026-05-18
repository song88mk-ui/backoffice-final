using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Pages.Base;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class StaticTranslation : BasePage
    {
        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public TranslationState TranslationState { get; set; }

        private PageHeaderModel _pageHeaderModel = new PageHeaderModel()
        {
            Title = "Sportsbook Static Translation",
            IconClass = "ico ico-translations"
        };

        private Dictionary<string, string> Translations { get; set; }
        private string SearchText { get; set; } = string.Empty;

        private string SelectedLanguageCode { get; set; } = "EN";

        private IEnumerable<KeyValuePair<string, string>> FilteredTranslations =>
            string.IsNullOrWhiteSpace(SearchText)
                ? Translations
                : Translations.Where(t => t.Key.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                          t.Value.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        protected override async Task OnInitializedAsync()
        {
            var response = await BackofficeService.GetStaticTranslationSportsbook("EN");
            if (response != null && response.StatusCode == StatusCode.Success && response.Result != null)
            {
                Translations = response.Result.ToDictionary(t => t.Name, t => t.Value);
            }
            else
            {
                Translations = new Dictionary<string, string>();
            }

            await base.OnInitializedAsync();
        }

        private async Task OnLanguageChanged(ChangeEventArgs e)
        {
            if (e.Value != null)
            {
                SelectedLanguageCode = e.Value.ToString();
                var response = await BackofficeService.GetStaticTranslationSportsbook(SelectedLanguageCode);
                if (response != null && response.StatusCode == StatusCode.Success && response.Result != null)
                {
                    Translations = response.Result.ToDictionary(t => t.Name, t => t.Value);
                }
                else
                {
                    Translations = new Dictionary<string, string>();
                }
            }
        }

        private async Task OnAddTranslation()
        {
            var parameters = new ModalParameters();
            parameters.Add("TranslationKey", string.Empty);
            parameters.Add("TranslationValue", string.Empty);
            parameters.Add("IsNew", true);
            parameters.Add("OnSaveCallback", (Func<string, string, Task>)(async (key, newValue) => await SaveTranslationValue(key, newValue)));
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");

            var options = new ModalOptions();
            ModalService.Open<StaticTranslationEditModal>("Add Translation", parameters, options);
        }

        private async Task OnEditTranslation(KeyValuePair<string, string> translation)
        {
            var parameters = new ModalParameters();
            parameters.Add("TranslationKey", translation.Key);
            parameters.Add("TranslationValue", translation.Value);
            parameters.Add("IsNew", false);
            parameters.Add("OnSaveCallback", (Func<string, string, Task>)(async (key, newValue) => await SaveTranslationValue(key, newValue)));
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");

            var options = new ModalOptions();
            ModalService.Open<StaticTranslationEditModal>("Edit Translation", parameters, options);
        }

        private async Task OnDeleteTranslation(KeyValuePair<string, string> translation)
        {
            var parameters = new ModalParameters();
            parameters.Add("TranslationKey", translation.Key);
            parameters.Add("OnDeleteCallback", (Func<Task>)(async () => await DeleteTranslationValue(translation.Key)));
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");

            var options = new ModalOptions();
            ModalService.Open<StaticTranslationDeleteModal>("Delete Translation", parameters, options);
        }

        private Task SaveTranslationValue(string key, string newValue)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return Task.CompletedTask;
            }

            Translations[key] = newValue;
            return InvokeAsync(StateHasChanged);
        }

        private Task DeleteTranslationValue(string key)
        {
            if (Translations.ContainsKey(key))
            {
                Translations.Remove(key);
            }

            return InvokeAsync(StateHasChanged);
        }
    }
}