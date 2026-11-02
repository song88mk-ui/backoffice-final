using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Backoffice.Web.Helpers
{
    /// <summary>
    /// Builds markup for MarkupString rendering without letting stored or external text turn into HTML.
    /// Encode every value interpolated into a markup string; keep the tags themselves as literals.
    /// </summary>
    public static class MarkupHelper
    {
        // The only markup notification producers send: bold, line breaks, and a plain http(s) link
        // (Backoffice.Utility excel download, Backoffice.API boosted settings).
        private static readonly Regex AllowedMarkup = new Regex(
            @"<(?<tag>b|/b|strong|/strong|br\s*/?)>|<a\s[^>]*?href\s*=\s*""(?<href>https?://[^""<>\s]+)""[^>]*>(?<text>[^<]*)</a>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));

        public static string Encode(object? value) => WebUtility.HtmlEncode(value?.ToString() ?? string.Empty);

        /// <summary>
        /// Encodes the whole message except &lt;b&gt;, &lt;strong&gt;, &lt;br&gt; and &lt;a href="http(s)://..."&gt;text&lt;/a&gt;,
        /// which are rebuilt from scratch so no other attribute (onclick, onerror, style) survives.
        /// </summary>
        public static string ToSafeMarkup(string? message)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;

            try
            {
                var result = new StringBuilder();
                var position = 0;

                foreach (Match match in AllowedMarkup.Matches(message))
                {
                    result.Append(Encode(message.Substring(position, match.Index - position)));

                    if (match.Groups["href"].Success)
                        result.Append($"<a href=\"{Encode(match.Groups["href"].Value)}\">{Encode(match.Groups["text"].Value)}</a>");
                    else
                    {
                        var tag = match.Groups["tag"].Value.ToLowerInvariant();
                        result.Append(tag.StartsWith("br") ? "<br>" : $"<{tag}>");
                    }

                    position = match.Index + match.Length;
                }

                result.Append(Encode(message.Substring(position)));

                return result.ToString();
            }
            catch (RegexMatchTimeoutException)
            {
                return Encode(message);
            }
        }
    }
}
