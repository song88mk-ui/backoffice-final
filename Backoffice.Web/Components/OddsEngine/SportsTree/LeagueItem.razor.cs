using Backoffice.Web.Models.OddsEngine.Monitoring;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.SportsTree
{
    public partial class LeagueItem
    {
        [Parameter] public LeagueItemModel League { get; set; }
    }
}
