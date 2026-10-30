using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;

namespace Backoffice.WebSocket.API.Security
{
    /// <summary>
    /// Identifies the calling service by its x-api-key header (Publisher or Subscriber), and the
    /// Back Office user by the Auth API JWT the Web sends as a Bearer token. The user id for
    /// notifications comes only from that validated token, never from the query string.
    /// </summary>
    public class HubApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "HubApiKey";
        public const string ApiKeyHeader = "x-api-key";

        private readonly HubSecurityOptions _security;

        public HubApiKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, HubSecurityOptions security)
            : base(options, logger, encoder)
        {
            _security = security;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var apiKey = Request.Headers[ApiKeyHeader].FirstOrDefault();
            if (string.IsNullOrEmpty(apiKey))
                return AuthenticateResult.NoResult();

            string role;
            if (KeyMatches(apiKey, _security.PublisherApiKey))
                role = HubRoles.Publisher;
            else if (KeyMatches(apiKey, _security.SubscriberApiKey))
                role = HubRoles.Subscriber;
            else
                return AuthenticateResult.Fail("Unsupported API key.");

            var identity = new ClaimsIdentity(SchemeName, ClaimTypes.Name, ClaimTypes.Role);
            identity.AddClaim(new Claim(ClaimTypes.Name, role));
            identity.AddClaim(new Claim(ClaimTypes.Role, role));

            var authorization = Request.Headers[HeaderNames.Authorization].FirstOrDefault();
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var userId = await ValidateUserTokenAsync(authorization["Bearer ".Length..].Trim());
                if (userId == null)
                    return AuthenticateResult.Fail("Invalid user token.");

                identity.AddClaim(new Claim(HubClaims.UserId, userId));
            }

            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
        }

        private async Task<string> ValidateUserTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(_security.JwtKey) || string.IsNullOrWhiteSpace(_security.JwtIssuer))
                return null;

            // Same parameters the Auth API itself validates with (HS256, issuer = audience).
            var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_security.JwtKey)),
                ValidateIssuer = true,
                ValidIssuer = _security.JwtIssuer,
                ValidateAudience = true,
                ValidAudience = _security.JwtIssuer,
                ValidateLifetime = true
            });

            if (!result.IsValid)
                return null;

            var userId = result.ClaimsIdentity.Claims.FirstOrDefault(x => string.Equals(x.Type, HubClaims.UserId, StringComparison.OrdinalIgnoreCase))?.Value;
            return string.IsNullOrEmpty(userId) ? null : userId;
        }

        private static bool KeyMatches(string provided, string expected) =>
            !string.IsNullOrEmpty(expected)
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected));
    }
}
