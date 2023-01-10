using Backoffice.Shared.Enums;
using Backoffice.Shared.Utilities;
using Backoffice.Web.ActionFilter;
using Backoffice.Web.Hubs;
using Backoffice.Web.Middleware;
using Backoffice.Web.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

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
    options.AddPolicy("sbc-menu-topleagues", policy => policy.RequireClaim("SportsbookClient.TopLeagues.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-menu-topfixtures", policy => policy.RequireClaim("SportsbookClient.TopFixtures.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-menu-parlaybonus", policy => policy.RequireClaim("SportsbookClient.ParlayBonus.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("sbc-menu-subuser", policy => policy.RequireClaim("MainUserId",
        ""));
    #endregion

    #region Trading
    options.AddPolicy("trading-menu-monitoring-match", policy => policy.RequireClaim("Trading.Monitoring.Match.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("trading-menu-monitoring-outrights", policy => policy.RequireClaim("Trading.Monitoring.Outrights.Access",
            $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("trading-menu-config-match", policy => policy.RequireClaim("Trading.Config.Match.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("trading-menu-config-outrights", policy => policy.RequireClaim("Trading.Config.Outrights.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("trading-menu-administration-managemanualmessagesettlement", policy => policy.RequireClaim("Trading.Administration.ManageManualMessageSettlement.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("trading-menu-administration-usergroup", policy => policy.RequireClaim("Trading.Administration.ManageUserGroup.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("trading-menu-priceboost-manageboost", policy => policy.RequireClaim("Trading.PriceBoost.ManageBoost.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    options.AddPolicy("trading-menu-priceboost-managesettings", policy => policy.RequireClaim("Trading.PriceBoost.BoostedSettings.Access",
        $"{(int)Access.Edit}", $"{(int)Access.View}"));
    #endregion
});

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddSession();

builder.Services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
{
    client.BaseAddress = new Uri(configuration["API:Backoffice:Uri"]);
    client.DefaultRequestHeaders.Clear();
    client.DefaultRequestHeaders.Add("x-api-key", configuration["API:Backoffice:Security:APIKey"]);
});

builder.Services.AddScoped<IModalService, ModalService>();
builder.Services.AddHttpClient<ITradingService, TradingService>();
builder.Services.AddHttpClient<IBackOfficeService, BackOfficeService>();
builder.Services.AddScoped<ICommonService, CommonService>();
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();
builder.Services.AddSingleton<IMessageBroker, MessageBroker>();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

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
