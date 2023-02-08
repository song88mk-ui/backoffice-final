using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Enums
{
    public enum PermissionCategory
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
        SBC_Reports_DrillDownReport,
        SBC_Reports_OpenBets,
        SBC_Reports_SearchBetId,
        SBC_Reports_SearchUserId,
        SBC_Reports_SearchEvent,
        SBC_Reports_MonitorUsers,
        SBC_Reports_Sports,
        SBC_Reports_MarketReport,
        SBC_Reports_CLTV,
        SBC_Reports_Performance,
        SBC_Settings_TopLeagues,
        SBC_Settings_TopFixtures,
        SBC_Settings_ParlayBonus,
        SBC_SubUsers,
        SBC_Clients,
        SBC_Clients_Access,
        SBC_OE,
        SBC_OE_Monitoring_MatchType,
        SBC_OE_Monitoring_Outrights,
        SBC_OE_FastOdds,
        SBC_OE_Config,
        SBC_OE_Settlement_Market,
        SBC_OE_Settlement_Message,
        SBC_OE_SportsSettings,
        SBC_OE_MarketSetting_SortOrder,
        SBC_OE_MarketSetting_Group,
        SBC_OE_PriceBoost_Manage,
        SBC_OE_PriceBoost_Settings
    }
}
