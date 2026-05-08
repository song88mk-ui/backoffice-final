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
            Console.WriteLine($"Selected Language Code: {e.Value}");
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
    }
}