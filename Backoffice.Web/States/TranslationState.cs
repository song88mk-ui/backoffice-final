using Backoffice.Shared.Enums;
using Backoffice.Web.Services;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.JSInterop;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Backoffice.Web.States
{
    public class TranslationState : BaseState<Dictionary<string, string>>
    {
        private ITranslationService _translationService;
        private readonly IJSRuntime _jsRuntime;
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

        public TranslationState(ITranslationService translationService, IJSRuntime jsRuntime) : base(new Dictionary<string, string>())
        {
            _translationService = translationService;
            _jsRuntime = jsRuntime;
            LanguageCodes = Enum.GetValues<LanguageCode>().Select(s => s).ToList();

            if (!Model.Any())
                ReloadModel();
        }

        public string GetValue(string word)
        {
            if (string.IsNullOrEmpty(word))
                return word;

            var key = word.Replace(" ", "_").ToLower();
            var placeHolders = ExtractPlaceholders(ref key);
            var translated = Model.TryGetValue(key, out string? value) ? value : word;

            if (placeHolders.Any() && translated.Contains("{&}"))
                return TransformPlaceHolder(translated, "{&}", placeHolders.ToArray());

            return translated.Replace("{", "").Replace("}", "");
        }

        private async void ReloadModel() => Model = await _translationService.GetTranslationsAsync(_activeLangCode);
        private string TransformPlaceHolder(string word, string placeholder, params object[] values)
        {
            var index = -1;
            var i = 0;

            while ((index = word.IndexOf(placeholder)) != -1 && i < values.Length)
            {
                word = word.Substring(0, index) + values[i] + word.Substring(index + placeholder.Length);
                i++;
            }

            return word;
        }
        private List<string> ExtractPlaceholders(ref string input)
        {
            List<string> result = new List<string>();
            Regex regex = new Regex(@"\{(.*?)\}");

            MatchCollection matches = regex.Matches(input);
            foreach (Match match in matches)
            {
                var matchVal = $"{match.Groups[1].Value}";
                input = input.Replace($"{{{matchVal}}}", "{&}");
                result.Add(matchVal);
            }

            return result;
        }
    }
}
