using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class LeagueAccordion
    {
        [Parameter] public SportSettingLeague League { get; set; }
        [Parameter] public EditSportSettingsPage Page { get; set; }

        private EditSportSettingsPage _page;

        protected override void OnParametersSet()
        {
            _page = Page;

            base.OnParametersSet(page);
        }
    }
}
