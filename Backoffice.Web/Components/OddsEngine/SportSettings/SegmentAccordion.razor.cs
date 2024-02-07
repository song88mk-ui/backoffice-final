using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class SegmentAccordion
    {
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter] public SportSettingSegment Segment { get; set; }
        [Parameter] public EditSportSettingsPage Page { get; set; }

        private SportSettingSegment _segment;

        private EditSportSettingsPage _page;

        private bool _showLeagues;
        private bool _initLeagueSortable;

        protected override void OnParametersSet()
        {
            _segment = Segment;
            _page = Page;

            base.OnParametersSet();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_page == EditSportSettingsPage.SegmentAndLeagues)
            {
                if (_showLeagues && !_initLeagueSortable)
                {
                    var id = $"#leagueEditable-{Segment.SportId}-{Segment.SegmentId}";

                    await JSRuntime.InvokeVoidAsync("enableSortOrder2", id, $"{id} .league-container", Segment.SportId, Segment.SegmentId);
                    _initLeagueSortable = true;
                }

            }
        }

        public async Task OnToggleClick()
        {
            _showLeagues = !_showLeagues;

            if (!_showLeagues && _page == EditSportSettingsPage.SegmentAndLeagues)
            {
                await JSRuntime.InvokeVoidAsync("disableSortOrder2", $"#leagueEditable-{Segment.SportId}-{Segment.SegmentId}");
                _initLeagueSortable = false;
            }
        }
    }
}
