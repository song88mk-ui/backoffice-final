using Backoffice.WebSocket.API.Hubs;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(options =>
{
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(1); // Timeout for client inactivity
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);    // Server's keep-alive ping interval
});
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

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
