using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Backoffice.Shared.Enums;
using Microsoft.IdentityModel.Tokens;

namespace Backoffice.Web.Services;

public class MetabaseService
{
    private readonly string _siteUrl;
    private readonly string _ssoSecretKey;
    private readonly int _dashboardId;
    private readonly string _emailDomain;

    public string SiteUrl => _siteUrl;
    public int DashboardId => _dashboardId;
    public string EmailDomain => _emailDomain;

    public MetabaseService(IConfiguration configuration)
    {
        _siteUrl = configuration["Metabase:SiteUrl"]!;
        _ssoSecretKey = configuration["Metabase:SsoSecretKey"]!;
        _dashboardId = int.Parse(configuration["Metabase:DashboardId"]!);
        _emailDomain = configuration["Metabase:EmailDomain"]!;
    }

    public string GetSsoToken(string username, IEnumerable<string> clientCodes)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_ssoSecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var displayName = username.Split('@')[0];
        var email = $"{displayName}@{_emailDomain}";
        var now = DateTimeOffset.UtcNow;
        var payload = new JwtPayload
        {
            { "email", email },
            { "first_name", displayName },
            { "last_name", displayName },
            { "clientcode", (clientCodes ?? Enumerable.Empty<string>()).ToArray() },
            { "groups", new[] { "Clients" } },
            { "iat", now.ToUnixTimeSeconds() },
            { "exp", now.AddHours(1).ToUnixTimeSeconds() }
        };

        var token = new JwtSecurityToken(new JwtHeader(credentials), payload);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string ToLocale(LanguageCode langCode) => langCode switch
    {
        LanguageCode.ZH => "zh-CN",
        LanguageCode.KO => "ko",
        _ => "en"
    };
}
