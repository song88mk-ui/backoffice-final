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
    public enum PermissionCategoryAcronym
    {
        Admin,
        SBC,
        PMS,
        OE
    }
    public enum AdminPermission
    {
        Admin_Dashboard,
        Admin_ManageUsers,
        Admin_ManageUserGroups,
        Admin_Settlements_ManualBetSettlement,
        Admin_Settlements_ResendSettlement,
        Admin_Settlements_MarketSettlement,
        Admin_Settlements_MessageSettlement,
        Admin_CancelBets,
        Admin_CancelBetsHistory,
        Admin_Maintenance
    }
    public enum PartnerManagementPermission
    {
        PMS_Dashboard,
        PMS_ManagePartner,
        PMS_Setting,
        PMS_Bet_Parlay,
        PMS_Bet_All,
        PMS_Payment_Transfer,
        PMS_Payment_Settlement
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
