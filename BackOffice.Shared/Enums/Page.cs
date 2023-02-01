using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Backoffice.Shared.Enums
{
    public enum Page
    {
        [Description("Manage Boost")]
        OE_PriceBoost_Manage,
        [Description("Boosted Settings")]
        OE_PriceBoost_Settings,
        OE_Administration_ManageUserGroups,
        OE_Administration_ManageUsers,
        Admin_Settlement_Bet,
        Admin_Settlement_Resend,
        Admin_Settlement_Market,
        Admin_Settlement_Message,
        OE_Administration_MessageSettlement,
        OE_Administration_SportsSettings,
        OE_Administration_MarketSortOrder,
        OE_Administration_MarketGroup,
        OE_Config,
        Admin_ManageUserGroups,
        Admin_CancelBets
    }
}
