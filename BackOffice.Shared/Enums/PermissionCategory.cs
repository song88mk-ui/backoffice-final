using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Enums
{
    public enum PermissionCategory
    {
        Admin,
        Sportsbook,
        PartnerManagement,
        OddsEngine
    }
    public enum AdminPermission
    {
        Admin_Dashboard,
        Admin_Manage_User,
        Admin_Manage_UserGroup,
        Admin_Settlement_Bet,
        Admin_Settlement_Resend,
        Admin_Settlement_Market,
        Admin_Settlement_Message,
        Admin_Bet_Cancel,
        Admin_Bet_History,
        Admin_Maintenance
    }
    public enum PartnerManagementPermission
    {
        PartnerManagement_Dashboard,
        PartnerManagement_ManagePartner,
        PartnerManagement_Setting,
        PartnerManagement_Bet_Parlay,
        PartnerManagement_Bet_All,
        PartnerManagement_Payment_Transfer,
        PartnerManagement_Payment_Settlement
    }
    public enum SportsbookClientPermission
    {
        SportsbookClient_Dashboard,
        SportsbookClient_Report_DrillDown,
        SportsbookClient_Report_OpenBets,
        SportsbookClient_Report_Bet_Search,
        SportsbookClient_Report_User_Search,
        SportsbookClient_Report_User_Monitor,
        SportsbookClient_Report_Monitoring,
        SportsbookClient_Report_Sports,
        SportsbookClient_Setting_TopLeagues,
        SportsbookClient_Setting_TopFixtures,
        SportsbookClient_Setting_Parlay,
        SportsbookClient_SubUsers,
        SportsbookClient_Clients,
        SportsbookClient_Trading,
        SportsbookClient_Monitoring_Match,
        SportsbookClient_Monitoring_Outrights,
        SportsbookClient_FastOdds,
        SportsbookClient_Config_Match,
        SportsbookClient_Config_Outrights,
        SportsbookClient_SportsSettings,
        SportsbookClient_MarketSetting_SortOrder,
        SportsbookClient_MarketSetting_Group,
        SportsbookClient_PriceBoost_Manage,
        SportsbookClient_PriceBoost_Settings
    }
}
