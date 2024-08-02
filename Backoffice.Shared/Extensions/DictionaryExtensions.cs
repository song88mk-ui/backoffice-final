using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Backoffice.Shared.Extensions
{
    public static class DictionaryExtensions
    {
        public static string GetValue(this Dictionary<string,string> model, string word)
        {
            var key = word.Replace(" ", "_").ToLower();
            var placeHolders = ExtractPlaceholders(ref key);
            var translated = model.TryGetValue(key, out string? value) ? value : word;

            if (placeHolders.Any() && translated.Contains("{&}"))
                return TransformPlaceHolder(translated, "{&}", placeHolders.ToArray());

            return translated.Replace("{", "").Replace("}", "");
        }

        private static string TransformPlaceHolder(string word, string placeholder, params object[] values)
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

        private static List<string> ExtractPlaceholders(ref string input)
        {
            var result = new List<string>();
            var regex = new Regex(@"\{(.*?)\}");

            var matches = regex.Matches(input);

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
