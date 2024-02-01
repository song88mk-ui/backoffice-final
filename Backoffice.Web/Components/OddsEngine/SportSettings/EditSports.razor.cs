using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Pages.SBC;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class EditSports
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter] public List<SportSettingSport> SportSettings { get; set; }
        [Parameter] public List<SportSettingSport> SportLookupSettings { get; set; }

        private List<SportSettingSport> _sportSettings = new List<SportSettingSport>();
        private List<SportSettingSport> _sportLookupSettings = new List<SportSettingSport>();

        private List<int> selectedSportIds = new List<int>();

        private SearchFormModel _searchForm = new SearchFormModel();

        private bool _isSortable;
        private bool _selectAll;
        private bool _removeAll;

        private bool _shouldRender = true;

        protected override Task OnParametersSetAsync()
        {
            var activeSportIds = SportSettings.Where(c => c.IsActive).Select(s => s.SportId).ToList();

            //_sportSettings = SportLookupSettings.Where(c => activeSportIds.Contains(c.SportId))
            //    .Join(SportSettings,
            //    lookup => lookup.SportId,
            //    sport => sport.SportId,
            //    (lookup, sport) => new SportSettingSport
            //    {
            //        SportId = sport.SportId,
            //        s
            //        SortOrder = sport.SortOrder,
            //        IsActive = true,
            //    })
            //    .OrderBy(o => o.SortOrder)
            //    .ToList();

            _sportSettings = SportSettings;

            _sportLookupSettings = SportLookupSettings.Where(c => !activeSportIds.Contains(c.SportId)).ToList();

            _shouldRender = true;

            return base.OnParametersSetAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JSRuntime.InvokeVoidAsync("loadEventListener", DotNetObjectReference.Create(this));
            }

            if (!_isSortable && _sportLookupSettings.Any())
            {
                await JSRuntime.InvokeVoidAsync("enableSportSortOrder");
                _isSortable = true;
            }
        }

        protected override bool ShouldRender() => _shouldRender;

        public async Task OnSelectAllClick()
        {
            _selectAll = true;

            _shouldRender = true;
        }

        public async Task OnClearSelectionClick()
        {
            _selectAll = false;

            _shouldRender = true;
        }

        public async Task OnRemoveClick()
        {
            if (!selectedSportIds.Any())
                return;

            _sportSettings = _sportSettings.Where(c => !selectedSportIds.Contains(c.SportId))
                .Select((s, index) =>
                {
                    s.SortOrder = index + 1;

                    return s;
                })
                .ToList();

            var selectedSports = SportLookupSettings.Where(c => selectedSportIds.Contains(c.SportId)).ToList();
            var sortOrder = _sportSettings.Max(c => c.SortOrder) + 1;

            foreach (var sport in selectedSports)
            {
                sport.SortOrder = sortOrder;

                _sportLookupSettings.Add(sport);

                sortOrder++;
            }

            selectedSportIds = new List<int>();

            _shouldRender = true;
        }

        public async Task OnClearSportsSelectionClick()
        {
            if (!selectedSportIds.Any())
                return;

            _removeAll = false; 

            _shouldRender = true;
        }

        public async Task OnCheckboxChanged(Tuple<int, bool> data)
        {
            if (data.Item2)
                selectedSportIds.Add(data.Item1);
            else
                selectedSportIds.Remove(data.Item1);

            _shouldRender = false;
        }

        private async Task OnInputSearchEvent()
        {
            if (!string.IsNullOrEmpty(_searchForm.SearchText) && _searchForm.SearchText.Length == 1)
                return;

            if (string.IsNullOrEmpty(_searchForm.SearchText))
            {
                var activeSportIds = SportSettings.Where(c => c.IsActive).Select(s => s.SportId).ToList();

                _sportLookupSettings = SportLookupSettings.Where(c => !activeSportIds.Contains(c.SportId))
                    .OrderBy(o => o.SportName)
                    .ToList();
            }
            else
            {
                _sportLookupSettings = SportLookupSettings.Where(c => c.SportName.Contains(_searchForm.SearchText))
                    .OrderBy(o => o.SportName)
                    .ToList();
            }

            _shouldRender = true;
        }

        private async Task OnAddToSportstreeClick()
        {
            var sportIds = (await JSRuntime.InvokeAsync<List<string>>("getSelectedSports"))
                .Select(int.Parse)
                .ToList();

            if (!sportIds.Any())
                return;

            var selectedSports = _sportLookupSettings.Where(c => sportIds.Contains(c.SportId)).ToList();

            var sortOrder = _sportSettings.Max(c => c.SortOrder) + 1;

            foreach (var sport in selectedSports)
            {
                sport.SortOrder = sortOrder;

                _sportSettings.Add(sport);

                sortOrder++;
            }

            _sportLookupSettings = _sportLookupSettings.Where(c => !sportIds.Contains(c.SportId)).ToList();

            _shouldRender = true;
        }

        [JSInvokable]
        public async void UpdateSportSetting(List<SortOrderModel> sports, List<SortOrderModel> allSports)
        {
            // need to remove check as well
            var sportIds = sports.Select(s => int.Parse(s.RowId)).ToList();

            _sportSettings = SportLookupSettings.Where(c => sportIds.Contains(c.SportId))
                .Join(sports,
                setting => setting.SportId,
                sort => int.Parse(sort.RowId),
                (setting, sort) => new SportSettingSport
                {
                    SportId = setting.SportId,
                    SportName = setting.SportName,
                    Segments = setting.Segments,
                    SortOrder = sort.SortOrder,
                    IsActive = true
                })
                .OrderBy(o => o.SortOrder)
                .ToList();

            var remainingSportIds = allSports.Select(s => int.Parse(s.RowId)).ToList();

            _sportLookupSettings = SportLookupSettings.Where(c => remainingSportIds.Contains(c.SportId))
                .Join(allSports,
                setting => setting.SportId,
                sort => int.Parse(sort.RowId),
                (setting, sort) => new SportSettingSport
                {
                    SportId = setting.SportId,
                    SportName = setting.SportName,
                    Segments = setting.Segments,
                    SortOrder = sort.SortOrder
                })
                .OrderBy(o => o.SortOrder)
                .ToList();

            _selectAll = false;
            _shouldRender = true;

            await InvokeAsync(StateHasChanged);
        }
    }
}
