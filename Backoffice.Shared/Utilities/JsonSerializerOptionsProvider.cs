using System.Text.Json;

namespace Backoffice.Shared.Utilities
{
    /// <summary>
    /// Provides cached, reusable <see cref="JsonSerializerOptions"/> instances.
    ///
    /// System.Text.Json builds and caches type metadata on the
    /// <see cref="JsonSerializerOptions"/> instance the first time a type is
    /// (de)serialized with it. Passing a freshly constructed options object to every
    /// Serialize/Deserialize call discards that cache every time and is dramatically
    /// slower. Reuse a single instance instead.
    /// See https://www.meziantou.net/avoid-performance-issue-with-jsonserializer-by-reusing-the-same-instance-of-json.htm
    /// </summary>
    public static class JsonSerializerOptionsProvider
    {
        /// <summary>
        /// Shared options with case-insensitive property-name matching. The instance
        /// is configured once and then only read, so it is safe to reuse concurrently.
        /// </summary>
        public static readonly JsonSerializerOptions CaseInsensitive = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
