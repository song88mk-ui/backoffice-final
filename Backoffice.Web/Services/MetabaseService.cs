using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Backoffice.Web.Services;

public class MetabaseService
{
    private readonly string _siteUrl;
    private readonly string _ssoSecretKey;
    private readonly int _dashboardId;
    private readonly string _email;

    public string SiteUrl => _siteUrl;
    public int DashboardId => _dashboardId;

    public MetabaseService(IConfiguration configuration)
    {
        _siteUrl = configuration["Metabase:SiteUrl"]!;
        _ssoSecretKey = configuration["Metabase:SsoSecretKey"]!;
        _dashboardId = int.Parse(configuration["Metabase:DashboardId"]!);
        _email = configuration["Metabase:Email"]!;
    }
    
    public string GetSsoToken()
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_ssoSecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var now = DateTimeOffset.UtcNow;
        var payload = new JwtPayload
        {
            { "email", _email },
            { "iat", now.ToUnixTimeSeconds() },
            { "exp", now.AddHours(1).ToUnixTimeSeconds() }
        };

        var token = new JwtSecurityToken(new JwtHeader(credentials), payload);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
