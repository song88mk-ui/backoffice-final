using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using System.Security.Claims;

namespace Backoffice.Web.Helpers
{
    public static class MenuHelper
    {
        public static async Task<MenuModel> GetMenu(Claim claim, string name)
        {
            var menu = new MenuModel
            {
                Id = $"menu.{claim.Type.ToLower()}",
                Name = claim.Value == "2" ? $"{name} (Readonly)" : $"{name}",
            };

            //ADMIN
            if (claim.Type == AdminPermission.Admin_Dashboard.ToString().Replace('_', '.'))
            {
                menu.IconName = "dashboard";
                menu.Url = "admin/dashboard";
            }
            else if (claim.Type == AdminPermission.Admin_ManageUsers.ToString().Replace('_', '.'))
            {
                menu.IconName = "manage-user";
                menu.Url = "admin/manage-user";
            }
            else if (claim.Type == AdminPermission.Admin_ManageUserGroups.ToString().Replace('_', '.'))
            {
                menu.IconName = "manage-user-group";
                menu.Url = "admin/manage-usergroup";
            }
            else if (claim.Type == AdminPermission.Admin_Settlements_ManualBetSettlement.ToString().Replace('_', '.'))
            {
                menu.IconName = "manual-bet-settlement";
                menu.Url = "admin/manual-bet-settlement";
            }
            else if (claim.Type == AdminPermission.Admin_Settlements_ResendSettlement.ToString().Replace('_', '.'))
            {
                menu.IconName = "resend-settlement";
                menu.Url = "admin/resend-settlement";
            }
            else if (claim.Type == AdminPermission.Admin_Settlements_MarketSettlement.ToString().Replace('_', '.'))
            {
                menu.IconName = "market-settlement";
                menu.Url = "admin/market-settlement";
            }
            else if (claim.Type == AdminPermission.Admin_Settlements_MessageSettlement.ToString().Replace('_', '.'))
            {
                menu.IconName = "message-settlement";
                menu.Url = "admin/message-settlement";
            }
            else if (claim.Type == AdminPermission.Admin_CancelBets.ToString().Replace('_', '.'))
            {
                menu.IconName = "cancel-bets";
                menu.Url = "admin/cancel-bets";
            }
            else if (claim.Type == AdminPermission.Admin_CancelBetsHistory.ToString().Replace('_', '.'))
            {
                menu.IconName = "cancel-bet-history";
                menu.Url = "admin/cancel-bet-history";
            }
            else if (claim.Type == AdminPermission.Admin_Maintenance.ToString().Replace('_', '.'))
            {
                menu.IconName = "maintenance";
                menu.Url = "admin/maintenance";
            }

            //SBC
            if (claim.Type == SportsbookClientPermission.SBC_Dashboard.ToString().Replace('_', '.'))
            {
                menu.IconName = "dashboard";
                menu.Url = "/dashboard";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_DrillDownReport.ToString().Replace('_', '.'))
            {
                menu.IconName = "drill-down-report";
                menu.Url = "/drill-down-report";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_OpenBets.ToString().Replace('_', '.'))
            {
                menu.IconName = "open-bets";
                menu.Url = "/open-bets";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_SearchBetId.ToString().Replace('_', '.'))
            {
                menu.IconName = "search-bet";
                menu.Url = "/search-bet";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_SearchUserId.ToString().Replace('_', '.'))
            {
                menu.IconName = "search-user";
                menu.Url = "/search-user";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_SearchEvent.ToString().Replace('_', '.'))
            {
                menu.IconName = "search-event";
                menu.Url = "/search-event";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_MonitorUsers.ToString().Replace('_', '.'))
            {
                menu.IconName = "monitor-users";
                menu.Url = "/monitor-users";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_Sports.ToString().Replace('_', '.'))
            {
                menu.IconName = "sports";
                menu.Url = "/sports";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_MarketReport.ToString().Replace('_', '.'))
            {
                menu.IconName = "market-report";
                menu.Url = "/market-report";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_CLTV.ToString().Replace('_', '.'))
            {
                menu.IconName = "cltv";
                menu.Url = "/cltv";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_Performance.ToString().Replace('_', '.'))
            {
                menu.IconName = "performance";
                menu.Url = "/performance";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Settings_TopLeagues.ToString().Replace('_', '.'))
            {
                menu.IconName = "top-leagues";
                menu.Url = "/top-leagues";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Settings_TopFixtures.ToString().Replace('_', '.'))
            {
                menu.IconName = "top-fixtures";
                menu.Url = "/top-fixtures";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Settings_ParlayBonus.ToString().Replace('_', '.'))
            {
                menu.IconName = "parlay-bonus";
                menu.Url = "/parlay-bonus";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_SubUsers.ToString().Replace('_', '.'))
            {
                menu.IconName = "sub-user";
                menu.Url = "/sub-user";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Clients.ToString().Replace('_', '.'))
            {
                menu.IconName = "clients";
                menu.Url = "clients";
            }


            return menu;
        }
    }
}
