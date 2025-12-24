using Backoffice.WebSocket.API.Hubs;
using Backoffice.WebSocket.API.Telemetry;
using MessagePack;
using MessagePack.Resolvers;
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

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.MapHub<BackofficeHub>("/bohub", options =>
{
    options.ApplicationMaxBufferSize = 5000000;
    options.TransportMaxBufferSize = 5000000;
    options.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
});
app.Run();
