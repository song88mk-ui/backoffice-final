using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Backoffice.Web.Services;

public class MetabaseService
{
    private readonly string _siteUrl;
    private readonly string _secretKey;
    private readonly string _ssoSecretKey;
    private readonly int _dashboardId;

    public string SiteUrl => _siteUrl;
    public int DashboardId => _dashboardId;

    public MetabaseService(IConfiguration configuration)
    {
        _siteUrl = configuration["Metabase:SiteUrl"]!;
        _secretKey = configuration["Metabase:SecretKey"]!;
        _ssoSecretKey = configuration["Metabase:SsoSecretKey"] ?? _secretKey;
        _dashboardId = int.Parse(configuration["Metabase:DashboardId"]!);
    }

    // Legacy signed embedding — kept as fallback
    public string GetEmbedUrl(List<string> clientCodes)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var payload = new JwtPayload
        {
            { "resource", new { dashboard = _dashboardId } },
            { "params", new { client = clientCodes.ToArray() } }
        };

        var token = new JwtSecurityToken(new JwtHeader(credentials), payload);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return $"{_siteUrl}/embed/dashboard/{tokenString}#bordered=true&titled=true";
    }

    // JWT SSO token for Interactive Embedding
    // The SsoSecretKey must match the key configured in Metabase Admin → Authentication → JWT
    public string GetSsoToken(string username)
    {
        var email = $"{username.ToLower().Replace(" ", ".")}@song88.com";
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_ssoSecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var now = DateTimeOffset.UtcNow;
        var payload = new JwtPayload
        {
            { "email", "devops@song88.com" }, // Todo: replace with actual email derived from memCache username
            { "first_name", "DevOps" },
            { "last_name", "DevOps" },
            { "iat", now.ToUnixTimeSeconds() },
            { "exp", now.AddHours(1).ToUnixTimeSeconds() }
        };

        var token = new JwtSecurityToken(new JwtHeader(credentials), payload);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
