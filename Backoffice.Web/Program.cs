using Azure.Identity;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Extensions;
using Backoffice.Web.Services;
using Backoffice.Web.Telemetry;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.ResponseCompression;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Services.BuildServiceProvider().GetService<IConfiguration>();

//
// Telemetry
Action<ResourceBuilder> configureResource = r => r.AddService(
    serviceName: "Backoffice.Web",
    serviceNamespace: builder.Configuration["Telemetry:Environment"],
    serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "latest",
    serviceInstanceId: Environment.MachineName);

builder.Services.AddSingleton<Instrumentation>();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

Log.Logger = logger;

if (Convert.ToBoolean(builder.Configuration["Telemetry:Enabled"]))
{
    builder.Services.AddOpenTelemetry()
      .ConfigureResource(configureResource)
      .WithTracing(tracing =>
      {
          tracing
          .AddSource(Instrumentation.ActivitySourceName)
          .SetSampler(new AlwaysOnSampler())
          .AddHttpClientInstrumentation(
            (opts) => opts.FilterHttpRequestMessage =
              (req) =>
              {
                  var uriString = req.RequestUri.ToString();
                  var isAzureReq = uriString.Contains("azure.com") ||
                                   uriString.Contains("visualstudio.com");
                  return !isAzureReq;
              }
          )
          .AddAspNetCoreInstrumentation((opts) => opts.Filter = httpContext =>
          {
            var path = httpContext.Request.Path.ToString();
            var toExclude = path.EndsWith(".js") ||
                            path.EndsWith(".css") ||
                            path.StartsWith("/_blazor") ||
                            path.StartsWith("/_content") ||
                            path.StartsWith("/bohub") ||
                            path.StartsWith("/healthz");
            return !toExclude;
          })
          .AddSqlClientInstrumentation();

          builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(builder.Configuration.GetSection("Telemetry:AspNetCoreInstrumentation"));

          tracing.AddOtlpExporter(otlpOptions =>
          {
              otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
              otlpOptions.Endpoint = new Uri(builder.Configuration["Telemetry:Collector"] + "/v1/traces");
          });

      })
      .WithMetrics(metrics =>
      {
          metrics
          .AddMeter(Instrumentation.MeterName)
          .AddRuntimeInstrumentation()
          .AddHttpClientInstrumentation()
          .AddAspNetCoreInstrumentation();

          metrics.AddOtlpExporter(otlpOptions =>
          {
              otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
              otlpOptions.Endpoint = new Uri(builder.Configuration["Telemetry:Collector"] + "/v1/metrics");
          });

      });
}
// end of telemetry

// Add services to the container.
builder.Services.AddHealthChecks();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddCircuitOptions(o =>
{
    o.DetailedErrors = true;
});

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CONTAINER_APP_ENV_DNS_SUFFIX")))
{
    var envName = Environment.GetEnvironmentVariable("DPK_ENVIRONMENT");
    var dpkUri = $"https://song88dpk.blob.core.windows.net/{envName}/bo-web/dpk.xml";
    builder.Services.AddDataProtection()
        .PersistKeysToAzureBlobStorage(new Uri(dpkUri), new DefaultAzureCredential());
}


builder.Services.AddSingleton<IMemCache, MemCache>();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

builder.AddStates()
    .AddAuthorization()
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
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession();

//app.UseHttpsRedirection();
app.MapHealthChecks("/healthz");

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub(o => { o.Transports = HttpTransportType.WebSockets; });
app.MapFallbackToPage("/_Host");

app.Run();
