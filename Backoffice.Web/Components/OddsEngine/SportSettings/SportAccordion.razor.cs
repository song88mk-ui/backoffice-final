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
        [Parameter] public EventCallback<Tuple<int, bool>> OnCheckboxChangedCallback {get; set; }
        [Parameter] public EditSportSettingsPage Page { get; set; }

        private SportSettingSport _sport;

        private EditSportSettingsPage _page;

        private bool _showSegments;
        private bool _isSelected;
        private bool _initSegmentSortable;

        private bool _shouldRender = true;

        protected override Task OnInitializedAsync()
        {
            _sport = Sport;
            _page = Page;
            _isSelected = IsSelected;

            return base.OnInitializedAsync();
        }

        protected override void OnParametersSet()
        {
            _shouldRender = _page == Page && _isSelected == IsSelected ? false : true;

            if (!_shouldRender)
                return;

            _page = Page;
            _isSelected = IsSelected;

            base.OnParametersSet();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_page == EditSportSettingsPage.SegmentAndLeagues)
            {
                if (_showSegments && !_initSegmentSortable)
                {
                    var id = $"#segmentEditable-{Sport.SportId}";

                    await JSRuntime.InvokeVoidAsync("enableSortOrder2", id, $"{id} .segment-container", Sport.SportId, 0);
                    _initSegmentSortable = true;
                }
            }
        }

        protected override bool ShouldRender() => _shouldRender;

        public async Task OnToggleClick()
        {
            _showSegments = !_showSegments;

            if (!_showSegments && _page == EditSportSettingsPage.SegmentAndLeagues)
            {
                await JSRuntime.InvokeVoidAsync("disableSortOrder2", $"#segmentEditable-{Sport.SportId}");
                _initSegmentSortable = false;
            }

            _shouldRender = true;
        }

        public async Task OnCheckboxChanged(ChangeEventArgs e)
        {
            _isSelected = bool.Parse(e.Value.ToString());

            await OnCheckboxChangedCallback.InvokeAsync(new Tuple<int, bool>(Sport.SportId, _isSelected));

            _shouldRender = true;
        }
    }
}
