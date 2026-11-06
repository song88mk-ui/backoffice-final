using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using Serilog;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    // BO-155: the logs of Config > Assigned and Unassigned. Every assign, change of state (Active or Restricted) and
    // unassign is written to the activity log by Backoffice.API (SetConfigState, BO-151) as one row per league, on the
    // page SBC.OE.Config.Leagues, with the payload "{clientCode}.{sportId}.{segmentId}.{leagueId}" and the old and new
    // state in From and To. The row holds ids only, so the league name comes from the league lookup, one call per sport
    // and segment on the page. Only rows written after BO-155 are on that page; earlier ones were saved under the page
    // the web service had last set and are not shown.
    public partial class ViewLeagueLogs
    {
        #region Fields
        // The same name as BackofficeRepository.ConfigLeagueActivityLogPage in Backoffice.API. A test in each project
        // pins the value, so the two cannot drift apart without a test failing.
        public const string LogPage = "SBC.OE.Config.Leagues";

        private const string AllValue = "-1";
        private const int PageSize = 20;

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        private List<SelectListItemModel<string>> _clients = new();
        private List<SelectListItemModel<string>> _sports = new();
        private string _selectedClient = AllValue;
        private string _selectedSport = AllValue;
        private SearchFormModel _searchForm = new();
        private DateTime _dateFrom;
        private DateTime _dateTo;
        private MetaData _metaData = new();
        private List<LeagueLogRow> _rows = new();
        private AlertModel _alertModel = new();
        private bool _isPreLoader = true;
        private bool _loadFailed;

        // (sport, segment) -> league id -> league name, filled as the pages are read
        private readonly Dictionary<(int SportId, int SegmentId), Dictionary<int, string>> _leagueNames = new();

        private sealed class LeagueLogRow
        {
            public string Date { get; init; }
            public string Client { get; init; }
            public string Sport { get; init; }
            public string League { get; init; }
            public string Change { get; init; }
            public string From { get; init; }
            public string To { get; init; }
            public string UpdatedBy { get; init; }
        }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            var clients = ModalParameters?.TryGet<List<SelectListItemWithCheckboxModel<string>>>("Clients") ?? new List<SelectListItemWithCheckboxModel<string>>();
            var sports = ModalParameters?.TryGet<List<SelectListItemModel<string>>>("Sports") ?? new List<SelectListItemModel<string>>();

            _clients = new List<SelectListItemModel<string>> { new() { Value = AllValue, Name = "All Clients" } };
            _clients.AddRange(clients
                .Where(c => !c.IsSelectAll && !string.Equals(c.Value, "All", StringComparison.OrdinalIgnoreCase))
                .Select(c => new SelectListItemModel<string> { Value = c.Value, Name = c.Name }));

            _sports = new List<SelectListItemModel<string>> { new() { Value = AllValue, Name = "All Sports" } };
            _sports.AddRange(sports);

            _metaData = new MetaData { CurrentPage = 1, PageSize = PageSize };

            // the same day boundaries the other View Logs dialogs use: the day before and today, in the user's zone
            _dateFrom = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddDays(-1);
            _dateTo = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddDays(1).AddTicks(-1);

            await GetContent();
        }

        public async Task SelectPage(int page)
        {
            _metaData.CurrentPage = page;
            await GetContent();
        }

        public async Task OnClientChanged(ChangeEventArgs e)
        {
            _selectedClient = e?.Value?.ToString() ?? AllValue;
            await SelectPage(1);
        }

        public async Task OnSportChanged(ChangeEventArgs e)
        {
            _selectedSport = e?.Value?.ToString() ?? AllValue;
            await SelectPage(1);
        }

        public async Task OnDatePickerRangeSelected(DateRange range)
        {
            _dateFrom = range.Start.Date;
            _dateTo = range.End.Date.AddDays(1).AddTicks(-1);

            await SelectPage(1);
        }

        public async Task OnSearchSubmitted() => await SelectPage(1);

        public void OnCloseClicked() => _modalService.Cancel();
        #endregion

        #region Utilities
        // BO-105: TimeStamp is stored in UTC, so it is shown in the zone the date filter uses.
        private string FormatTimeStamp(DateTime timeStamp) =>
            (timeStamp + _userState.TimeZoneOffset).ToString("dd/MM/yy HH:mm");

        private string BuildPayload() =>
            $"{(_selectedClient != AllValue ? _selectedClient.ToUpperInvariant() : "%%")}." +
            $"{(_selectedSport != AllValue ? _selectedSport : "%%")}.%%.%%";

        // The row stores the client code only, so a typed client name or code is turned into the codes it matches and
        // the API matches those against the payload (the same approach as the Client Site Config logs, BO-104).
        private List<string> GetMatchingClientCodes(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return new List<string>();

            return _clients
                .Where(c => c.Value != AllValue
                    && ((c.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (c.Value?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)))
                .Select(c => c.Value.ToUpperInvariant())
                .ToList();
        }

        private async Task GetContent()
        {
            if (!_isPreLoader)
            {
                _isPreLoader = true;
                StateHasChanged();
            }

            var response = await _backofficeService.GetActivityLogs(new ActivityLogRequest
            {
                Page = LogPage,
                Payload = BuildPayload(),
                MetaData = _metaData,
                DateFrom = _dateFrom,
                DateTo = _dateTo,
                SearchText = _searchForm.SearchText,
                PayloadSearchTerms = GetMatchingClientCodes(_searchForm.SearchText)
            });

            if (response != null && response.StatusCode == StatusCode.Success && response.Result != null)
            {
                _loadFailed = false;
                _alertModel = new AlertModel();
                _metaData = response.Result.MetaData ?? _metaData;
                _rows = await BuildRows(response.Result.ActivityLogs ?? new List<ActivityLogModel>());
            }
            else
            {
                // A failed read must not look like "no record", so the table stays empty under an error message.
                Log.Warning("OE Config could not load the league logs: {Message}", response?.Message);

                _loadFailed = true;
                _rows = new List<LeagueLogRow>();
                _metaData = new MetaData { CurrentPage = 1, PageSize = PageSize };
                _alertModel = new AlertModel
                {
                    Status = AlertStatus.Error,
                    Message = "The logs could not be loaded. Please try again, or contact support if it keeps happening.",
                    IsAlwaysShown = true
                };
            }

            _isPreLoader = false;
        }

        private async Task<List<LeagueLogRow>> BuildRows(List<ActivityLogModel> logs)
        {
            var parsed = logs.Select(log => (Log: log, Key: TryParsePayload(log.Payload))).ToList();

            foreach (var pair in parsed.Where(p => p.Key.HasValue).Select(p => (p.Key.Value.SportId, p.Key.Value.SegmentId)).Distinct())
                await LoadLeagueNames(pair.SportId, pair.SegmentId);

            return parsed.Select(p =>
            {
                var key = p.Key;

                return new LeagueLogRow
                {
                    Date = FormatTimeStamp(p.Log.TimeStamp),
                    Client = key.HasValue ? GetClientName(key.Value.ClientCode) : "-",
                    Sport = key.HasValue ? GetSportName(key.Value.SportId) : "-",
                    // A payload that is not in the expected shape is shown as it is, never dropped.
                    League = key.HasValue ? GetLeagueName(key.Value.SportId, key.Value.SegmentId, key.Value.LeagueId) : p.Log.Payload,
                    Change = GetChange(p.Log.Activity),
                    From = p.Log.From,
                    To = p.Log.To,
                    // SetConfigState takes the name from the UserName claim, which the Auth tokens do not carry (they have
                    // TokenName), so the rows it writes have a user id and no name; a dash then, not an empty cell.
                    UpdatedBy = string.IsNullOrWhiteSpace(p.Log.UserName) ? "-" : p.Log.UserName
                };
            }).ToList();
        }

        // "{clientCode}.{sportId}.{segmentId}.{leagueId}"
        private static (string ClientCode, int SportId, int SegmentId, int LeagueId)? TryParsePayload(string payload)
        {
            var parts = payload?.Split('.');

            if (parts == null || parts.Length != 4
                || string.IsNullOrWhiteSpace(parts[0])
                || !int.TryParse(parts[1], out var sportId)
                || !int.TryParse(parts[2], out var segmentId)
                || !int.TryParse(parts[3], out var leagueId))
                return null;

            return (parts[0], sportId, segmentId, leagueId);
        }

        private string GetClientName(string clientCode)
        {
            var client = _clients.FirstOrDefault(c => c.Value != AllValue && string.Equals(c.Value, clientCode, StringComparison.OrdinalIgnoreCase));

            return client != null ? $"{client.Name} ({clientCode})" : clientCode;
        }

        private string GetSportName(int sportId)
        {
            var sport = _sports.FirstOrDefault(s => s.Value == sportId.ToString());

            return sport != null ? _translationState.GetValue(sport.Name) : $"Sport {sportId}";
        }

        private string GetLeagueName(int sportId, int segmentId, int leagueId) =>
            _leagueNames.TryGetValue((sportId, segmentId), out var names) && names.TryGetValue(leagueId, out var name)
                ? name
                : $"League {leagueId}";

        // Create is a league that had no saved state before, so it was assigned. Update is a change of state, Delete an unassign.
        private string GetChange(string activity) => activity switch
        {
            nameof(Activity.Create) => _translationState.GetValue("Assigned"),
            nameof(Activity.Update) => _translationState.GetValue("State changed"),
            nameof(Activity.Delete) => _translationState.GetValue("Unassigned"),
            _ => activity
        };

        private async Task LoadLeagueNames(int sportId, int segmentId)
        {
            if (_leagueNames.ContainsKey((sportId, segmentId)))
                return;

            try
            {
                var response = await _backofficeService.GetLeagueLookup(sportId, segmentId);

                if (response != null && response.StatusCode == StatusCode.Success && response.Result?.LeagueLookup != null)
                {
                    _leagueNames[(sportId, segmentId)] = response.Result.LeagueLookup
                        .GroupBy(l => l.LeagueId)
                        .ToDictionary(g => g.Key, g => g.First().LeagueName);
                }
                else
                {
                    // not stored, so the next page tries again; the rows show "League {id}" meanwhile
                    Log.Warning("OE Config could not read the league names of sport {SportId} segment {SegmentId}: {Message}", sportId, segmentId, response?.Message);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "OE Config could not read the league names of sport {SportId} segment {SegmentId}", sportId, segmentId);
            }
        }
        #endregion
    }
}
