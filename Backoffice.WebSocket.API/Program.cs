using Backoffice.WebSocket.API.Extensions;
using Backoffice.WebSocket.API.Hubs;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServerSideBlazor()
        .AddCircuitOptions(options => options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3));

builder.Services.AddSignalR(options => options.MaximumReceiveMessageSize = 5000000);
;
builder.Services.AddResponseCompression(opts => opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" }));

builder.Services.AddHealthChecks();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(opts => { opts.SingleLine = true; });

builder.Services.AddTelemetry(builder.Configuration);

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.MapHub<BackofficeHub>("/bohub", options => {
    options.ApplicationMaxBufferSize = 5000000;
    options.TransportMaxBufferSize = 5000000;
    options.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
});
app.Run();
