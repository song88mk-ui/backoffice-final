using Backoffice.Shared.Enums;
using Backoffice.Shared.Utilities;
using Backoffice.Web.ActionFilter;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components.Authorization;

namespace Backoffice.Web.Extensions
{
    public static class WebApplicationBuilderExtensions
    {
        public static WebApplicationBuilder AddAuthorization(this WebApplicationBuilder builder)
        {
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("disabled", policy => policy.RequireClaim($"-D!5@bl3d-"));

                #region SBC

                options.AddPolicy("sbc-dashboard", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Dashboard.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-drilldown", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_DrillDownReport.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-openbets", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_OpenBets.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-search-betid", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_SearchBetId.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-search-userid", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_SearchUserId.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-search-event", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_SearchEvent.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-monitor-users", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_MonitorUsers.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-sports", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_Sports.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-market-report", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_MarketReport.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-cltv", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_CLTV.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-performance", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Reports_Performance.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));

                options.AddPolicy("sbc-topleagues", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Settings_TopLeagues.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-topleagues-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Settings_TopLeagues.ToString().Replace('_', '.')}", $"{(int)Access.Edit}"));
                options.AddPolicy("sbc-topfixtures", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Settings_TopFixtures.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-topfixtures-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Settings_TopFixtures.ToString().Replace('_', '.')}", $"{(int)Access.Edit}"));
                options.AddPolicy("sbc-parlaybonus", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Settings_ParlayBonus.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-parlaybonus-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Settings_ParlayBonus.ToString().Replace('_', '.')}", $"{(int)Access.Edit}"));
                options.AddPolicy("sbc-announcements", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Settings_Announcements.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-announcements-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Settings_Announcements.ToString().Replace('_', '.')}", $"{(int)Access.Edit}"));
                options.AddPolicy("sbc-subusers", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_SubUsers.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-subusers-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_SubUsers.ToString().Replace('_', '.')}", $"{(int)Access.Edit}"));
                options.AddPolicy("sbc-clients", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Clients.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-clients-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_Clients.ToString().Replace('_', '.')}", $"{(int)Access.Edit}"));

                options.AddPolicy("sbc-monitoring-match", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_Monitoring_MatchType.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-monitoring-match-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_Monitoring_MatchType.ToString().Replace('_', '.')}", $"{(int)Access.Edit}"));
                options.AddPolicy("sbc-monitoring-outright", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_Monitoring_Outrights.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));

                options.AddPolicy("sbc-config", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_Config.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-config-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_Config.ToString().Replace('_', '.')}", $"{(int)Access.Edit}"));
                options.AddPolicy("sbc-sports-setting", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_SportsSettings.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-sports-setting-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_SportsSettings.ToString().Replace('_', '.')}", $"{(int)Access.Edit}"));
                options.AddPolicy("sbc-market-sortorder", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_MarketSetting_SortOrder.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-market-sortorder-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_MarketSetting_SortOrder.ToString().Replace('_', '.')}", $"{(int)Access.Edit}"));

                options.AddPolicy("sbc-market-group", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_MarketSetting_Group.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));

                options.AddPolicy("sbc-priceboost-manage", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_PriceBoost_Manage.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-priceboost-manage-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_PriceBoost_Manage.ToString().Replace('_', '.')}", $"{(int)Access.Edit}"));
                options.AddPolicy("sbc-priceboost-settings", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_PriceBoost_Settings.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("sbc-priceboost-settings-canedit", policy => policy.RequireClaim($"{SportsbookClientPermission.SBC_OE_PriceBoost_Settings.ToString().Replace('_', '.')}", $"{(int)Access.Edit}"));

                #endregion

                #region ADMIN

                options.AddPolicy("admin-dashboard", policy => policy.RequireClaim($"{AdminPermission.Admin_Dashboard.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("admin-users", policy => policy.RequireClaim($"{AdminPermission.Admin_ManageUsers.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("admin-usergroups", policy => policy.RequireClaim($"{AdminPermission.Admin_ManageUserGroups.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("admin-settlement-manual", policy => policy.RequireClaim($"{AdminPermission.Admin_Settlements_ManualBetSettlement.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("admin-settlement-market", policy => policy.RequireClaim($"{AdminPermission.Admin_Settlements_MarketSettlement.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("admin-settlement-message", policy => policy.RequireClaim($"{AdminPermission.Admin_Settlements_MessageSettlement.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("admin-settlement-resend", policy => policy.RequireClaim($"{AdminPermission.Admin_Settlements_ResendSettlement.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("admin-cancel", policy => policy.RequireClaim($"{AdminPermission.Admin_CancelBets.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("admin-cancel-history", policy => policy.RequireClaim($"{AdminPermission.Admin_CancelBetsHistory.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));
                options.AddPolicy("admin-maintenance", policy => policy.RequireClaim($"{AdminPermission.Admin_Maintenance.ToString().Replace('_', '.')}", $"{(int)Access.Edit}", $"{(int)Access.View}"));

                #endregion
            });
            builder.Services.AddAuthorizationCore();

            return builder;
        }
        public static WebApplicationBuilder AddServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<IModalService, ModalService>();
            builder.Services.AddSingleton<IMessageBroker, MessageBroker>();

            builder.Services.AddScoped<ITableauService, TableauService>();
            builder.Services.AddScoped<IPreRenderService, PreRenderService>();

            builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();
            builder.Services.AddScoped<ITranslationService, TranslationService>();

            builder.Services.AddScoped<ToastService>();

            return builder;
        }

        public static WebApplicationBuilder AddHttpClients(this WebApplicationBuilder builder)
        {
            builder.Services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
            {
                client.BaseAddress = new Uri(builder?.Configuration["API:Backoffice:Uri"] ?? string.Empty);
                client.DefaultRequestHeaders.Add("x-api-key", builder?.Configuration["API:Backoffice:Security:APIKey"]);
            });
            builder.Services.AddHttpClient<ITradingService, TradingService>(client =>
            {
                client.BaseAddress = new Uri(builder?.Configuration["API:Backoffice:Uri"] ?? string.Empty);

                client.DefaultRequestHeaders.Add("x-api-key", builder?.Configuration["API:Backoffice:Security:APIKey"]);
                client.DefaultRequestHeaders.Add("x-region", builder?.Configuration["API:Region"]);
            });
            builder.Services.AddHttpClient<IBackofficeService, BackofficeService>(client =>
            {
                client.BaseAddress = new Uri(builder?.Configuration["API:Backoffice:Uri"] ?? string.Empty);
                client.DefaultRequestHeaders.Add("x-api-key", builder?.Configuration["API:Backoffice:Security:APIKey"]);
                client.DefaultRequestHeaders.Add("x-region", builder?.Configuration["API:Region"]);
            });
            return builder;
        }

        public static WebApplicationBuilder AddStates(this WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<TranslationState>();
            builder.Services.AddScoped<PageState>();

            return builder;
        }
    }
}
