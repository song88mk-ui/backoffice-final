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
        [Parameter] public EventCallback<int> OnSegmentToggleCallback { get; set; }

        //private SportSettingSegment _segment;

        private EditSportSettingsPage _page;

        private bool _isOpen;
        private bool _initLeagueSortable;
        private List<SportSettingLeague> _leagues = null;

        private int _sportId;
        private int _segmentId;
        private string _segmentName;

        protected override void OnParametersSet()
        {
            _sportId = Segment.SportId;
            _segmentId = Segment.SegmentId;
            _segmentName = Segment.SegmentName;
            _isOpen = Segment.IsOpen;

            if (_leagues == null || !_leagues.Equals(Segment.Leagues))
                _leagues = Segment.Leagues.OrderBy(x => x.SortOrder).ToList();

            _page = Page;

            base.OnParametersSet(page);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_page == EditSportSettingsPage.SegmentAndLeagues)
            {
                if (_isOpen && !_initLeagueSortable)
                {
                    var id = $"#leagueEditable-{Segment.SportId}-{Segment.SegmentId}";

                    await JSRuntime.InvokeVoidAsync("enableSortOrder2", id, $"{id} .league-container", Segment.SportId, Segment.SegmentId);
                    _initLeagueSortable = true;
                }
            }
        }

        public async Task OnToggleClick()
        {
            if (_isOpen && _page == EditSportSettingsPage.SegmentAndLeagues)
            {
                await JSRuntime.InvokeVoidAsync("disableSortOrder2", $"#leagueEditable-{Segment.SportId}-{Segment.SegmentId}");
                _initLeagueSortable = false;
            }

            await OnSegmentToggleCallback.InvokeAsync(_isOpen ? -1 : _segmentId);
        }
    }
}
