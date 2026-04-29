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
var configuration = builder.Services.BuildServiceProvider().GetService<IConfiguration>();

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
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(o => o.DetailedErrors = true);

// DPK TODO: configure dpk

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
if (!app.Environment.IsDevelopment())
{
    //app.UseExceptionHandler("/Error");
    //app.UseHsts();
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
}
// temp
app.UseDeveloperExceptionPage();

app.UseSession();

app.MapHealthChecks("/healthz");

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub(o => { o.Transports = HttpTransportType.WebSockets; });

// Metabase Interactive Embedding — JWT SSO token endpoint.
// Called by embed.js in the browser; protected by a short-lived one-time key stored in MemCache.
app.MapGet("/api/metabase/sso", (string key, IMemCache memCache, MetabaseService metabaseService) =>
{
    var username = memCache.GetCache<string>(key);
    if (string.IsNullOrEmpty(username))
        return Results.Unauthorized();

    var jwt = metabaseService.GetSsoToken(username);
    return Results.Ok(new { jwt });
});

app.MapFallbackToPage("/_Host");

app.Run();
