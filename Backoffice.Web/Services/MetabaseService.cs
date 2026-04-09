using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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

    public string GetEmbedUrl(string clientName)
    {
        var header = new { alg = "HS256", typ = "JWT" };
        var payload = new
        {
            resource = new { dashboard = _dashboardId },
            @params = new { client = new[] { clientName } },
            exp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()
        };

        var headerB64  = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header)));
        var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));

        var signingInput = $"{headerB64}.{payloadB64}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput)));

        return $"{_siteUrl}/embed/dashboard/{signingInput}.{signature}#bordered=true&titled=true";
    }

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
