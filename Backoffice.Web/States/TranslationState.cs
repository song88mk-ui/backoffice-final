using Backoffice.Shared.Enums;
using Backoffice.Web.Services;
using Backoffice.Web.States.Base;
using Microsoft.EntityFrameworkCore.Internal;
using System.Text.RegularExpressions;

namespace Backoffice.Web.States
{
    public class TranslationState : BaseState<Dictionary<string, string>>
    {
        #region Fields

        private ITranslationService _translationService;

        #endregion

        #region Properties

        public List<LanguageCode> LanguageCodes { get; set; }
        public LanguageCode ActiveLanguageCode { get; set; }

        #endregion

        #region Ctor

        public TranslationState(ITranslationService translationService) : base(new Dictionary<string, string>())
        {
            _translationService = translationService;

            LanguageCodes = new List<LanguageCode> { LanguageCode.EN, LanguageCode.ZH, LanguageCode.KO };

            if (!Model.Any())
                SetLanguageCode(LanguageCode.EN);
        }

        #endregion

        #region Methods

        public async Task SetLanguageCode(LanguageCode langCode)
        {
            ActiveLanguageCode = langCode;
            Model = await _translationService.GetTranslationsAsync(langCode);
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

        #endregion

        #region Utilities

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

        #endregion
    }
}
