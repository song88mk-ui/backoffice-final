using Backoffice.WebSocket.API.Hubs;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServerSideBlazor()
        .AddCircuitOptions(options => options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3));

builder.Services.AddSignalR(options => options.MaximumReceiveMessageSize = 5000000);
;
builder.Services.AddResponseCompression(opts => opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" }));

var app = builder.Build();

app.MapHub<BackofficeHub>("/bohub", options => {
    options.ApplicationMaxBufferSize = 5000000;
    options.TransportMaxBufferSize = 5000000;
    options.Transports = HttpTransportType.WebSockets;
});
app.Run();
