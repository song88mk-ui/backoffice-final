using Backoffice.Shared.Utilities;
using Backoffice.Web.Extensions;
using Backoffice.Web.Services;
using Backoffice.Web.Telemetry;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, serilog) =>
{
    serilog.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddSingleton<Instrumentation>();
builder.Services.AddTelemetry(new TelemetrySettings(
    ServiceName: Instrumentation.ActivitySourceName,
    ActivitySourceName: Instrumentation.ActivitySourceName,
    MeterName: Instrumentation.MeterName));

// Add services to the container.
builder.Services.AddHealthChecks();
builder.Services.AddRazorPages();
// CircuitOptions.DetailedErrors is bound by the framework from the DetailedErrors key (BO-132).
builder.Services.AddServerSideBlazor();

builder.AddDataProtectionKeys();

builder.Services.AddSingleton<IMemCache, MemCache>();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

builder.AddAuthorization()
    .AddHttpClients()
    .AddServices()
    .AddStates();

builder.Services.AddScoped<ICommonService, CommonService>();
//builder.Services.AddScoped<IExcelService, ExcelService>();

builder.Services.AddResponseCompression(opts => opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" }));

var app = builder.Build();

app.UseResponseCompression();

// Configure the HTTP request pipeline.
app.UseErrorPages();

app.UseSecurityHeaders();

app.UseSession();

app.MapHealthChecks("/healthz");

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub(o => { o.Transports = HttpTransportType.WebSockets; });

app.MapGet("/sso/metabase", (HttpContext context, MetabaseService metabaseService, IMemCache memCache) =>
{
    var userToken = context.Session.GetString("UserToken");
    if (string.IsNullOrWhiteSpace(userToken))
        return Results.Unauthorized();

    var claims = JwtParser.ParseClaimsFromJwt(userToken).ToList();
    var username = claims.FirstOrDefault(c => c.Type.ToUpper() == "TOKENNAME")?.Value ?? string.Empty;
    var userId = claims.FirstOrDefault(c => c.Type.ToUpper() == "USERID")?.Value ?? string.Empty;

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(userId))
        return Results.Unauthorized();

    var clientCodes = memCache.GetCache<List<string>>($"metabase-clients:{userId}")
        ?? claims.FirstOrDefault(c => c.Type.ToUpper() == "CLIENTCODES")?.Value
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
        ?? new List<string>();

    var jwt = metabaseService.GetSsoToken(username, clientCodes);
    return Results.Ok(new { jwt });
});


app.MapFallbackToPage("/_Host");

app.Run();
