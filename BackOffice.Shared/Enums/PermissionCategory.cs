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
        PMS_Dashboard,
        PartnerManagement_ManagePartner,
        PartnerManagement_Setting,
        PartnerManagement_Bet_Parlay,
        PartnerManagement_Bet_All,
        PartnerManagement_Payment_Transfer,
        PartnerManagement_Payment_Settlement
    }
    public enum SportsbookClientPermission
    {
        SBC_Dashboard,
        SBC_Report_DrillDown,
        SBC_Report_OpenBets,
        SBC_Report_Bet_Search,
        SBC_Report_User_Search,
        SBC_Report_User_Monitor,
        SBC_Report_Monitoring,
        SBC_Report_Sports,
        SBC_Settings_TopLeagues,
        SBC_Settings_TopFixtures,
        SBC_Settings_Parlay,
        SBC_SubUsers,
        SBC_Clients,
        SBC_Clients_Access,
        SBC_OE_Monitoring_Match,
        SBC_OE_Monitoring_Outrights,
        SBC_OE_FastOdds,
        SBC_OE_Config_Match,
        SBC_OE_Config_Outrights,
        SBC_OE_Settlement_Market,
        SBC_OE_Settlement_Message,
        SBC_OE_SportsSettings,
        SBC_OE_MarketSetting_SortOrder,
        SBC_OE_MarketSetting_Group,
        SBC_MarketSetting_Group,
        SBC_OE_PriceBoost_Manage,
        SBC_OE_PriceBoost_Settings
    }
}
