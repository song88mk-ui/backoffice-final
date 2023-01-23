using Backoffice.Shared.Enums;
using Backoffice.Shared.Utilities;
using Backoffice.Web.ActionFilter;
using Backoffice.Web.Hubs;
using Backoffice.Web.Middleware;
using Backoffice.Web.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.ResponseCompression;
using System.Drawing;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Services.BuildServiceProvider().GetService<IConfiguration>();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorizationCore();

builder.Services.AddAuthorization(options =>
{
    #region SBC
    options.AddPolicy("sbc-menu-topleagues", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Setting_TopLeagues.ToString().Replace('_', '.')}.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-menu-topfixtures", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Setting_TopFixtures.ToString().Replace('_', '.')}.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-menu-parlaybonus", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Setting_Parlay.ToString().Replace('_', '.')}.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-menu-subuser", policy => policy.RequireClaim("MainUserId",
        ""));
    #endregion

    #region Trading
    options.AddPolicy("trading-menu-monitoring-match", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_Monitoring_Match.ToString().Replace('_', '.')}.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("trading-menu-monitoring-outrights", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_Monitoring_Outrights.ToString().Replace('_', '.')}.Access",
            $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("trading-menu-config-match", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_Config_Match.ToString().Replace('_', '.')}.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("trading-menu-config-outrights", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_Config_Outrights.ToString().Replace('_', '.')}.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("trading-menu-settlement-market", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_Settlement_Message.ToString().Replace('_', '.')}.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("trading-menu-settlement-message", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_Settlement_Message.ToString().Replace('_', '.')}.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("trading-menu-priceboost-manage", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_PriceBoost_Manage.ToString().Replace('_', '.')}.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("trading-menu-priceboost-settings", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_PriceBoost_Settings.ToString().Replace('_', '.')} .Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    #endregion

    #region Admin
    options.AddPolicy("trading-menu-manage-user", policy => policy.RequireClaim($"{AdminPermission.Admin_Manage_User.ToString().Replace('_', '.')}.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    #endregion
});

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddSession();

builder.Services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
{
    client.BaseAddress = new Uri(configuration["API:Backoffice:Uri"]);
    client.DefaultRequestHeaders.Add("x-api-key", configuration["API:Backoffice:Security:APIKey"]);
});

builder.Services.AddScoped<IModalService, ModalService>();
builder.Services.AddHttpClient<ITradingService, TradingService>();
builder.Services.AddHttpClient<IBackOfficeService, BackOfficeService>();
builder.Services.AddScoped<ICommonService, CommonService>();
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();
builder.Services.AddSingleton<IMessageBroker, MessageBroker>();

builder.Services.AddScoped<IExcelService, ExcelService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});


var app = builder.Build();

app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseMiddleware<RabbitMQMiddleware>();

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseSession();

app.UseRouting();

app.MapBlazorHub();
app.MapHub<BoHub>("/bohub");
app.MapFallbackToPage("/_Host");

app.Run();
