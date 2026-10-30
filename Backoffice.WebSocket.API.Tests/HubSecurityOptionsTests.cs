using Backoffice.WebSocket.API.Security;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Backoffice.WebSocket.API.Tests
{
    public class HubSecurityOptionsTests
    {
        private static IConfiguration Config(params (string Key, string Value)[] values) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(values.ToDictionary(x => x.Key, x => x.Value))
                .Build();

        private static readonly (string, string)[] Complete =
        {
            ("Security:Enforce", "true"),
            ("Security:PublisherApiKey", "pub"),
            ("Security:SubscriberApiKey", "sub"),
            ("API:AuthenticationAPI:JWT:Key", "key"),
            ("API:AuthenticationAPI:JWT:Issuer", "bo.auth.api")
        };

        [Fact]
        public void Enforced_WithEverythingConfigured_Loads()
        {
            var options = HubSecurityOptions.FromConfiguration(Config(Complete));

            Assert.True(options.Enforce);
        }

        [Theory]
        [InlineData("Security:PublisherApiKey")]
        [InlineData("Security:SubscriberApiKey")]
        [InlineData("API:AuthenticationAPI:JWT:Key")]
        [InlineData("API:AuthenticationAPI:JWT:Issuer")]
        public void Enforced_WithASettingMissing_FailsAtStartup(string missing)
        {
            var values = Complete.Where(x => x.Item1 != missing).ToArray();

            Assert.Throws<InvalidOperationException>(() => HubSecurityOptions.FromConfiguration(Config(values)));
        }

        [Fact]
        public void Enforced_WithTheSameKeyForBothRoles_FailsAtStartup()
        {
            var values = Complete.Select(x => x.Item1 == "Security:SubscriberApiKey" ? (x.Item1, "pub") : x).ToArray();

            Assert.Throws<InvalidOperationException>(() => HubSecurityOptions.FromConfiguration(Config(values)));
        }

        [Fact]
        public void LogOnly_WithNothingConfigured_Loads()
        {
            var options = HubSecurityOptions.FromConfiguration(Config());

            Assert.False(options.Enforce);
        }
    }
}
