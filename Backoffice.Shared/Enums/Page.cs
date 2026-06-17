using System.ComponentModel;

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
        OE_Administration_ManualSettlement,
        OE_Administration_MessageSettlement,
        OE_Administration_SportsSettings,
        OE_Administration_MarketSortOrder,
        OE_Administration_MarketGroup,
        OE_Config,
    }
}
