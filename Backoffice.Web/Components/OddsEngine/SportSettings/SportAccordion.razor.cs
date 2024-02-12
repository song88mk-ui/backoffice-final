using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class SportAccordion
    {
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter] public SportSettingSport Sport { get; set; }
        [Parameter] public bool IsSelected { get; set; }
        [Parameter] public EventCallback<Tuple<int, bool>> OnCheckboxChangedCallback { get; set; }
        [Parameter] public EditSportSettingsPage Page { get; set; }
        [Parameter] public EventCallback<int> OnSportToggleCallback { get; set; }

        //private SportSettingSport _sport;

        private EditSportSettingsPage _page;

        private bool _isOpen;
        private bool _isSelected;
        private bool _initSegmentSortable;

        private bool _shouldRender = true;
        private int _sportId;
        private string _sportName;
        private List<SportSettingSegment> _segments = null;

        protected override Task OnInitializedAsync()
        {
            //_sport = Sport;
            _page = Page;
            _isSelected = IsSelected;

            _sportName = Sport.SportName;

            return base.OnInitializedAsync();
        }

        protected override void OnParametersSet()
        {
            _sportId = Sport.SportId;
            _sportName = Sport.SportName;

            if (_segments == null || !_segments.Equals(Sport.Segments))
                _segments = Sport.Segments.OrderBy(x => x.SortOrder).ToList();

            if (!Sport.IsOpen)
            {
                _segments.ForEach(x =>
                {
                    x.IsOpen = false;
                });
            }

            //_shouldRender = _page == Page && _isSelected == IsSelected ? false : true;

            //if (!_shouldRender)
            //    return;

            _page = Page;
            _isSelected = IsSelected;
            _isOpen = Sport.IsOpen;

            //base.OnParametersSet();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_page == EditSportSettingsPage.SegmentAndLeagues)
            {
                if (_isOpen && !_initSegmentSortable)
                {
                    var id = $"#segmentEditable-{Sport.SportId}";

                    await JSRuntime.InvokeVoidAsync("enableSortOrder2", id, $"{id} .segment-container", Sport.SportId, 0);
                    _initSegmentSortable = true;
                }
            }
        }

        //protected override bool ShouldRender() => _shouldRender;

        public async Task OnToggleClick()
        {
            //_showSegments = !_showSegments;

            if (_isOpen && _page == EditSportSettingsPage.SegmentAndLeagues)
            {
                await JSRuntime.InvokeVoidAsync("disableSortOrder2", $"#segmentEditable-{Sport.SportId}");
                _initSegmentSortable = false;
            }

            await OnSportToggleCallback.InvokeAsync(_isOpen ? -1 : _sportId);
            //_shouldRender = true;
        }
        private void OnSportAccordionSelectedEvent(int segmentId)
        {
            _segments.ForEach(x =>
            {
                if (segmentId == x.SegmentId)
                    x.IsOpen = true;
                else
                    x.IsOpen = false;
            });
        }

        public async Task OnCheckboxChanged(ChangeEventArgs e)
        {
            _isSelected = bool.Parse(e.Value.ToString());

            await OnCheckboxChangedCallback.InvokeAsync(new Tuple<int, bool>(Sport.SportId, _isSelected));

            _shouldRender = true;
        }
    }
}
