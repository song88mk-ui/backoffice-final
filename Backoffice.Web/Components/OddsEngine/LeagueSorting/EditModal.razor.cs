using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.LeagueSorting;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Components.OddsEngine.LeagueSorting
{
    public partial class EditModal : IDisposable
    {
        private const string TableSelector = ".table-edit-league-sorting";

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        private string _clientCode;
        private int _sportId;
        private string _sportName;

        private List<LeagueSortingEditRow> _rows = new List<LeagueSortingEditRow>();
        private List<SegmentModel> _segments = new List<SegmentModel>();
        private Guid? _openRowKey;
        private int _filterSegmentId;
        private int _filterLeagueId;
        private bool _isSaving;
        private AlertModel _alert;
        private DotNetObjectReference<EditModal> _dotNetRef;

        #region Methods

        protected override void OnInitialized()
        {
            _clientCode = ModalParameters.TryGet<string>("ClientCode");
            _sportId = ModalParameters.TryGet<int>("SportId");
            _sportName = ModalParameters.TryGet<string>("SportName");

            _rows = LeagueSortingHelper.ToEditRows(ModalParameters.TryGet<List<LeagueSortingLeague>>("CurrentLeagues") ?? new List<LeagueSortingLeague>());
        }

        protected override async Task OnInitializedAsync()
        {
            var language = _translationState.ActiveLanguageCode;

            var segmentTask = _backofficeService.GetSegmentLookup(_sportId, language);
            var leagueTask = _backofficeService.GetLeagueLookup(_sportId, 0, language);

            await Task.WhenAll(segmentTask, leagueTask);

            var segmentLookup = segmentTask.Result;
            var leagueLookup = leagueTask.Result;

            if (segmentLookup.StatusCode != StatusCode.Success || leagueLookup.StatusCode != StatusCode.Success
                || segmentLookup.Result == null || leagueLookup.Result == null)
            {
                _alert = new AlertModel { Status = AlertStatus.Error, Message = "Unable to load the leagues. Please close this window and try again." };
                return;
            }

            _segments = segmentLookup.Result.SegmentLookup
                .Where(s => s.SportId == _sportId)
                .Select(s => new SegmentModel
                {
                    SegmentId = s.SegmentId,
                    SegmentName = s.SegmentName,
                    Leagues = leagueLookup.Result.LeagueLookup
                        .Where(l => l.SportId == _sportId && l.SegmentId == s.SegmentId)
                        .Select(l => new LeagueModel { LeagueId = l.LeagueId, LeagueName = l.LeagueName })
                        .OrderBy(l => l.LeagueName)
                        .ToList()
                })
                .Where(s => s.Leagues.Any())
                .OrderBy(s => s.SegmentName)
                .ToList();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _dotNetRef = DotNetObjectReference.Create(this);
                await _jsRuntime.InvokeVoidAsync("enableSelectorTemplateSort", TableSelector, _dotNetRef, nameof(UpdateLeagueOrder));
            }
        }

        [JSInvokable]
        public async Task UpdateLeagueOrder(List<string> rowKeys)
        {
            // A mismatch means the browser and the model diverged; keep the model untouched.
            var ordered = LeagueSortingHelper.Reorder(_rows, rowKeys);

            if (ordered == null)
                return;

            _rows = ordered;
            await InvokeAsync(StateHasChanged);
        }

        public void OnDropdownClickEvent(LeagueSortingEditRow row)
        {
            _openRowKey = _openRowKey == row.Key ? null : row.Key;
        }

        public void OnDropdownCloseEvent()
        {
            _openRowKey = null;
        }

        public void OnLeagueSelectedEvent(LeagueSortingEditRow row, (SegmentModel Segment, LeagueModel League) selection)
        {
            row.SegmentId = selection.Segment.SegmentId;
            row.SegmentName = selection.Segment.SegmentName;
            row.LeagueId = selection.League.LeagueId;
            row.LeagueName = selection.League.LeagueName;

            _openRowKey = null;
            NormalizeFilters();
        }

        public void OnAddClickEvent(LeagueSortingEditRow row)
        {
            if (_rows.Count >= LeagueSortingHelper.MaxLeaguesPerClient)
                return;

            _rows.Insert(_rows.IndexOf(row) + 1, new LeagueSortingEditRow());
        }

        public void OnDeleteClickEvent(LeagueSortingEditRow row)
        {
            _rows.Remove(row);

            if (!_rows.Any())
                _rows.Add(new LeagueSortingEditRow());

            if (_openRowKey == row.Key)
                _openRowKey = null;

            NormalizeFilters();
        }

        public void OnFilterSegmentChangedEvent(ChangeEventArgs e)
        {
            _filterSegmentId = int.TryParse(e.Value?.ToString(), out var segmentId) ? segmentId : 0;
            _filterLeagueId = 0;
        }

        public void OnFilterLeagueChangedEvent(ChangeEventArgs e)
        {
            _filterLeagueId = int.TryParse(e.Value?.ToString(), out var leagueId) ? leagueId : 0;
        }

        public void OnCancelClickEvent()
        {
            _modalService.Cancel();
        }

        public async Task OnSaveClickEvent()
        {
            _alert = new AlertModel();

            var error = LeagueSortingHelper.ValidateRows(_rows);

            if (error != null)
            {
                _alert = new AlertModel { Status = AlertStatus.Error, Message = error };
                return;
            }

            _isSaving = true;

            var response = await _backofficeService.AddOrUpdateClientOddstableLeagueSettings(new ClientOddstableLeagueSettingRequest
            {
                ClientCode = _clientCode,
                SportId = _sportId,
                Leagues = LeagueSortingHelper.ToSortOrders(_sportId, _sportName, _rows),
                UserId = _userState.Model.Id,
                UserName = _userState.Model.Name,
                TimeStamp = DateTime.UtcNow
            });

            _isSaving = false;

            if (response.StatusCode == StatusCode.Success && response.Result?.StatusCode == StatusCode.Success)
            {
                _alert = new AlertModel { Status = AlertStatus.Success, Message = "League Sorting Saved!" };
                await InvokeAsync(StateHasChanged);

                await Modal.OnSaveClickCallback.InvokeAsync();
                await Task.Delay(1200);

                _modalService.Cancel();
                return;
            }

            _alert = new AlertModel
            {
                Status = AlertStatus.Error,
                Message = response.Result?.Message ?? response.Message ?? "Error when saving league sorting."
            };
        }

        public void Dispose()
        {
            _dotNetRef?.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        // The filters narrow what is listed in the dialog, so they only offer segments and leagues that are in it.
        private IEnumerable<(int SegmentId, string SegmentName)> FilterSegments =>
            _rows.Where(r => !r.IsEmpty).Select(r => (r.SegmentId, r.SegmentName)).Distinct();

        private IEnumerable<LeagueSortingEditRow> FilterLeagues =>
            _rows.Where(r => !r.IsEmpty && (_filterSegmentId == 0 || r.SegmentId == _filterSegmentId));

        // Rows still waiting for a league are always shown, so a freshly added row is never hidden by a filter.
        private bool IsRowVisible(LeagueSortingEditRow row) =>
            row.IsEmpty
            || ((_filterSegmentId == 0 || row.SegmentId == _filterSegmentId) && (_filterLeagueId == 0 || row.LeagueId == _filterLeagueId));

        private HashSet<(int SegmentId, int LeagueId)> TakenByOtherRows(LeagueSortingEditRow row) =>
            _rows.Where(r => r != row && !r.IsEmpty).Select(r => (r.SegmentId, r.LeagueId)).ToHashSet();

        // A filter that no longer matches any row (its league was changed or removed) goes back to ALL.
        private void NormalizeFilters()
        {
            if (_filterSegmentId != 0 && _rows.All(r => r.IsEmpty || r.SegmentId != _filterSegmentId))
            {
                _filterSegmentId = 0;
                _filterLeagueId = 0;
            }
            else if (_filterLeagueId != 0 && _rows.All(r => r.IsEmpty || r.LeagueId != _filterLeagueId))
            {
                _filterLeagueId = 0;
            }
        }

        #endregion
    }
}
