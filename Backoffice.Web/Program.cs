using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Utilities;
using Backoffice.Web.ActionFilter;
using Backoffice.Web.Hubs;
using Backoffice.Web.Middleware;
using Backoffice.Web.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using System.Text.Json;

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
    options.AddPolicy("dissabled", policy => policy.RequireClaim($"-D!5s@bl3d-"));
    options.AddPolicy("sbc-drilldown", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_DrillDownReport.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-openbets", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_OpenBets.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-search-betid", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_SearchBetId.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-search-userid", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_SearchUserId.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-search-event", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_SearchEvent.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-monitor-users", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_MonitorUsers.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-sports", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_Sports.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-market-report", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_MarketReport.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-cltv", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_CLTV.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-performance", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_Performance.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));

    options.AddPolicy("sbc-topleagues", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Settings_TopLeagues.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-topleagues-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Settings_TopLeagues.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}"));
    options.AddPolicy("sbc-topfixtures", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Settings_TopFixtures.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-topfixtures-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Settings_TopFixtures.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}"));
    options.AddPolicy("sbc-parlaybonus", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Settings_ParlayBonus.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-parlaybonus-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Settings_ParlayBonus.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}"));
    options.AddPolicy("sbc-subusers", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_SubUsers.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-subusers-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_SubUsers.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}"));
    options.AddPolicy("sbc-clients", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Clients.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-clients-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Clients.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}"));

    options.AddPolicy("sbc-monitoring-match", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_Monitoring_MatchType.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-monitoring-outright", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_Monitoring_Outrights.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));

    options.AddPolicy("sbc-config", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_Config.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-config-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_Config.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}"));
    options.AddPolicy("sbc-sports-setting", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_SportsSettings.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-sports-setting-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_SportsSettings.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}"));
    options.AddPolicy("sbc-market-sortorder", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_MarketSetting_SortOrder.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-market-sortorder-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_MarketSetting_SortOrder.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}"));

    options.AddPolicy("sbc-market-group", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_MarketSetting_Group.ToString().Replace('_', '.')}",
    $"{(int)Access.Edit}", $"{(int)Access.View}"));

    options.AddPolicy("sbc-priceboost-manage", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_PriceBoost_Manage.ToString().Replace('_', '.')}",
    $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-priceboost-manage-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_PriceBoost_Manage.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}"));
    options.AddPolicy("sbc-priceboost-settings", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_PriceBoost_Settings.ToString().Replace('_', '.')}",
    $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-priceboost-settings-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_PriceBoost_Settings.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}"));

    #endregion

    #region ADMIN
    options.AddPolicy("admin-users", policy => policy.RequireClaim($"{AdminPermission.Admin_ManageUsers.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("admin-usergroups", policy => policy.RequireClaim($"{AdminPermission.Admin_ManageUserGroups.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("admin-settlement-manual", policy => policy.RequireClaim($"{AdminPermission.Admin_Settlements_ManualBetSettlement.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("admin-settlement-market", policy => policy.RequireClaim($"{AdminPermission.Admin_Settlements_MarketSettlement.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("admin-settlement-message", policy => policy.RequireClaim($"{AdminPermission.Admin_Settlements_MessageSettlement.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("admin-settlement-resend", policy => policy.RequireClaim($"{AdminPermission.Admin_Settlements_ResendSettlement.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("admin-cancel", policy => policy.RequireClaim($"{AdminPermission.Admin_CancelBets.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("admin-cancel-history", policy => policy.RequireClaim($"{AdminPermission.Admin_CancelBetsHistory.ToString().Replace('_', '.')}",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("admin-maintenance", policy => policy.RequireClaim($"{AdminPermission.Admin_Maintenance.ToString().Replace('_', '.')}",
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
builder.Services.AddHttpClient<ITradingService, TradingService>(client =>
{
    client.BaseAddress = new Uri(configuration["API:Backoffice:Uri"]);

    client.DefaultRequestHeaders.Add("x-api-key", configuration["API:Backoffice:Security:APIKey"]);
    client.DefaultRequestHeaders.Add("x-region", configuration["API:Region"]);
});
builder.Services.AddHttpClient<IBackOfficeService, BackOfficeService>(client =>
{
    client.BaseAddress = new Uri(configuration["API:Backoffice:Uri"]);
    client.DefaultRequestHeaders.Add("x-api-key", configuration["API:Backoffice:Security:APIKey"]);
    client.DefaultRequestHeaders.Add("x-region", configuration["API:Region"]);
});
builder.Services.AddScoped<ICommonService, CommonService>();
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();
builder.Services.AddSingleton<IMessageBroker, MessageBroker>();
builder.Services.AddScoped<ToastService>();

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

app.UseSession();
app.UseMiddleware<RabbitMQMiddleware>();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapHub<BoHub>("/bohub");

app.Run();
