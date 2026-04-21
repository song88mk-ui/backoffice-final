using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Backoffice.Web.Services;

public class MetabaseService
{
    private readonly string _siteUrl;
    private readonly string _secretKey;
    private readonly int _dashboardId;

    public MetabaseService(IConfiguration configuration)
    {
        _siteUrl = configuration["Metabase:SiteUrl"]!;
        _secretKey = configuration["Metabase:SecretKey"]!;
        _dashboardId = int.Parse(configuration["Metabase:DashboardId"]!);
    }

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

        var embedUrl = $"{_siteUrl}/embed/dashboard/{tokenString}#bordered=true&titled=true";

        return embedUrl;
    }
    
}
