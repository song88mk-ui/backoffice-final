using Backoffice.Shared.Enums;
using Backoffice.Web.Services;
using Microsoft.EntityFrameworkCore.Internal;

namespace Backoffice.Web.States
{
    public class TranslationState : BaseState<Dictionary<string, string>>
    {
        private ITranslationService _translationService;

        public readonly List<LanguageCode> LanguageCodes;

        private LanguageCode _activeLangCode;
        public LanguageCode ActiveLanguageCode
        {
            get { return _activeLangCode; }
            set
            {
                if (value != _activeLangCode || !Model.Any())
                {
                    _activeLangCode = value;
                    ReloadModel();
                }
            }
        }

        public TranslationState(ITranslationService translationService) : base(new Dictionary<string, string>())
        {
            _translationService = translationService;

            LanguageCodes = Enum.GetValues<LanguageCode>().Select(s => s).ToList();

            if (!Model.Any())
                ReloadModel();
        }

        public string GetValue(string word, string placeholder = "#", params int[] values)
        {
            var result = GetValue(word);
            var index = -1;
            var i = 0;

            while ((index = result.IndexOf(placeholder)) != -1 && i < values.Length)
            {
                result = result.Substring(0, index) + values[i] + result.Substring(index + placeholder.Length);
                i++;
            }

            return result;
        }
        public string GetValue(string word) => Model.TryGetValue(word.Replace(" ", "_").ToLower(), out string? value) ? value : word;
        private async void ReloadModel()
        {
            Model = await _translationService.GetTranslations(_activeLangCode);
        }
    }
}
