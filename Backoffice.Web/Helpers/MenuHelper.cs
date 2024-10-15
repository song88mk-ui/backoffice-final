using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using System.Security.Claims;

namespace Backoffice.Web.Helpers
{
    public static class MenuHelper
    {
        public static MenuModel GetMenu(Claim claim, string name)
        {
            var (iconName, url) = claim.Type switch
            {
                var c when c == AdminPermission.Admin_Dashboard.ToString().Replace('_', '.') => ("dashboard", "admin/dashboard"),
                var c when c == AdminPermission.Admin_ManageUsers.ToString().Replace('_', '.') => ("manage-user", "admin/manage-user"),
                var c when c == AdminPermission.Admin_ManageUserGroups.ToString().Replace('_', '.') => ("manage-user-group", "admin/manage-usergroup"),
                var c when c == AdminPermission.Admin_Settlements_ManualBetSettlement.ToString().Replace('_', '.') => ("manual-bet-settlement", "admin/manual-bet-settlement"),
                var c when c == AdminPermission.Admin_Settlements_ResendSettlement.ToString().Replace('_', '.') => ("resend-settlement", "admin/resend-settlement"),
                var c when c == AdminPermission.Admin_Settlements_MarketSettlement.ToString().Replace('_', '.') => ("market-settlement", "admin/market-settlement"),
                var c when c == AdminPermission.Admin_Settlements_MessageSettlement.ToString().Replace('_', '.') => ("message-settlement", "admin/message-settlement"),
                var c when c == AdminPermission.Admin_CancelBets.ToString().Replace('_', '.') => ("cancel-bets", "admin/cancel-bets"),
                var c when c == AdminPermission.Admin_CancelBetsHistory.ToString().Replace('_', '.') => ("cancel-bet-history", "admin/cancel-bet-history"),
                var c when c == AdminPermission.Admin_Maintenance.ToString().Replace('_', '.') => ("maintenance", "admin/maintenance"),

                var c when c == SportsbookClientPermission.SBC_Dashboard.ToString().Replace('_', '.') => ("dashboard", "/dashboard"),
                var c when c == SportsbookClientPermission.SBC_Reports_DrillDownReport.ToString().Replace('_', '.') => ("drill-down-report", "/drill-down-report"),
                var c when c == SportsbookClientPermission.SBC_Reports_OpenBets.ToString().Replace('_', '.') => ("open-bets", "/open-bets"),
                var c when c == SportsbookClientPermission.SBC_Reports_SearchBetId.ToString().Replace('_', '.') => ("search-bet", "/search-bet"),
                var c when c == SportsbookClientPermission.SBC_Reports_SearchUserId.ToString().Replace('_', '.') => ("search-user", "/search-user"),
                var c when c == SportsbookClientPermission.SBC_Reports_SearchEvent.ToString().Replace('_', '.') => ("search-event", "/search-event"),
                var c when c == SportsbookClientPermission.SBC_Reports_MonitorUsers.ToString().Replace('_', '.') => ("monitor-users", "/monitor-users"),
                var c when c == SportsbookClientPermission.SBC_Reports_Sports.ToString().Replace('_', '.') => ("sports", "/sports"),
                var c when c == SportsbookClientPermission.SBC_Reports_MarketReport.ToString().Replace('_', '.') => ("market-report", "/market-report"),
                var c when c == SportsbookClientPermission.SBC_Reports_CLTV.ToString().Replace('_', '.') => ("cltv", "/cltv"),
                var c when c == SportsbookClientPermission.SBC_Reports_Performance.ToString().Replace('_', '.') => ("performance", "/performance"),
                var c when c == SportsbookClientPermission.SBC_Settings_TopLeagues.ToString().Replace('_', '.') => ("top-leagues", "/top-leagues"),
                var c when c == SportsbookClientPermission.SBC_Settings_TopFixtures.ToString().Replace('_', '.') => ("top-fixtures", "/top-fixtures"),
                var c when c == SportsbookClientPermission.SBC_Settings_ParlayBonus.ToString().Replace('_', '.') => ("parlay-bonus", "/parlay-bonus"),
                var c when c == SportsbookClientPermission.SBC_Settings_Announcements.ToString().Replace('_', '.') => ("announcements", "/announcements"),
                var c when c == SportsbookClientPermission.SBC_SubUsers.ToString().Replace('_', '.') => ("sub-user", "/sub-user"),
                var c when c == SportsbookClientPermission.SBC_Clients.ToString().Replace('_', '.') => ("clients", "clients"),

                _ => (string.Empty, string.Empty)
            };

            return new MenuModel
            {
                Id = $"menu.{claim.Type.ToLower()}",
                Name = claim.Value == "2" ? $"{name} (Readonly)" : $"{name}",
                IconName = iconName,
                Url = url
            };
        }
    }
}
