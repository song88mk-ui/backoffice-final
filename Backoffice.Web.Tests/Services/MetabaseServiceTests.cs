using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Backoffice.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Backoffice.Web.Tests.Services
{
    public class MetabaseServiceTests
    {
        // 64 characters, the same length as the deployed Metabase SSO keys.
        private const string ValidKey = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        private static readonly string[] TwoClientCodes = { "C1", "C2" };
        private static readonly string[] OneClientCode = { "C1" };

        private static MetabaseService BuildSut(string ssoSecretKey) =>
            new(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Metabase:SiteUrl"] = "https://metabase.example",
                    ["Metabase:SsoSecretKey"] = ssoSecretKey,
                    ["Metabase:DashboardId"] = "1",
                    ["Metabase:EmailDomain"] = "example.com"
                })
                .Build());

        [Fact]
        public void GetSsoToken_SignsHs256TokenThatValidatesWithTheSsoKey()
        {
            var token = BuildSut(ValidKey).GetSsoToken("jdoe@corp.local", TwoClientCodes);

            var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ValidKey)),
                ValidateIssuer = false,
                ValidateAudience = false
            }, out var validated);

            var jwt = Assert.IsType<JwtSecurityToken>(validated);
            Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Header.Alg);
            Assert.Equal("jdoe@example.com", jwt.Payload["email"]);
            Assert.Equal("jdoe", jwt.Payload["first_name"]);
            Assert.Equal(TwoClientCodes, principal.FindAll("clientcode").Select(c => c.Value));
            Assert.Equal("Clients", principal.FindFirst("groups")?.Value);
            Assert.InRange(jwt.ValidTo - DateTime.UtcNow, TimeSpan.FromMinutes(59), TimeSpan.FromMinutes(61));
        }

        [Fact]
        public void GetSsoToken_KeyShorterThan32Bytes_Throws()
        {
            // System.IdentityModel.Tokens.Jwt 6.34.0+ rejects HS256 keys under 256 bits (IDX10720). BO-119.
            var sut = BuildSut(new string('k', 16));

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => sut.GetSsoToken("jdoe@corp.local", OneClientCode));
            Assert.Contains("IDX10720", ex.Message);
        }
    }
}
