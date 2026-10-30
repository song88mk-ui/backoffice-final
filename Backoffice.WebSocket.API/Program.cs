using Backoffice.WebSocket.API.Hubs;
using Backoffice.WebSocket.API.Security;
using Backoffice.WebSocket.API.Telemetry;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
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

builder.Services.AddServerSideBlazor()
        .AddCircuitOptions(options => options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3));

builder.Services.AddSignalR(options => options.MaximumReceiveMessageSize = 5000000)
                .AddMessagePackProtocol(options =>
                {
                    StaticCompositeResolver.Instance.Register(
                        ContractlessStandardResolver.Instance
                    );
                    options.SerializerOptions = MessagePackSerializerOptions.Standard
                        .WithResolver(ContractlessStandardResolver.Instance)
                        .WithSecurity(MessagePackSecurity.UntrustedData);
                });

builder.Services.AddResponseCompression(opts => opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" }));

builder.Services.AddHealthChecks();

// BO-131: only the internal services connect, and only WebSocket.Service may publish.
var hubSecurity = HubSecurityOptions.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(hubSecurity);
builder.Services.AddAuthentication(HubApiKeyAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, HubApiKeyAuthenticationHandler>(HubApiKeyAuthenticationHandler.SchemeName, null);
builder.Services.AddSingleton<IAuthorizationHandler, HubAccessHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(HubPolicies.Connect, policy => policy.AddRequirements(new HubAccessRequirement()));
    options.AddPolicy(HubPolicies.Publish, policy => policy.AddRequirements(new HubAccessRequirement(HubRoles.Publisher)));
});

var app = builder.Build();

if (!hubSecurity.Enforce)
    app.Logger.LogWarning("Hub security is in log-only mode (Security:Enforce=false): callers without a valid API key are still accepted");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");

app.MapHub<BackofficeHub>("/bohub", options =>
{
    options.ApplicationMaxBufferSize = 5000000;
    options.TransportMaxBufferSize = 5000000;
    options.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
});
app.Run();

public partial class Program;
