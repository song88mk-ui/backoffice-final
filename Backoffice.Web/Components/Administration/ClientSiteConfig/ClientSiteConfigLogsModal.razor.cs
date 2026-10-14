using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Components.Administration.ClientSiteConfig
{
    public partial class ClientSiteConfigLogsModal
    {
        #region Fields
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        private List<PlatformClientResponse> _clients = new();
        private List<ActivityLogModel> _activityLogs = new();
        private MetaData _metaData;
        private SearchFormModel _searchForm = new();
        private DateTime _dateFrom;
        private DateTime _dateTo;

        private bool _isPreLoader = true;
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            _clients = ModalParameters.TryGet<List<PlatformClientResponse>>("Clients") ?? new List<PlatformClientResponse>();
            _metaData = new MetaData()
            {
                PageSize = 10,
                CurrentPage = 1
            };
            _dateFrom = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddDays(-7);
            _dateTo = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddDays(1).AddTicks(-1);

            await GetContent();
        }

        public async Task SelectPage(int page)
        {
            _metaData.CurrentPage = page;
            await GetContent();
        }

        public async Task OnDatePickerRangeSelected(DateRange range)
        {
            _dateFrom = range.Start.Date;
            _dateTo = range.End.Date.AddDays(1).AddTicks(-1);

            await SelectPage(1);
        }

        public async Task OnSearchSubmitted() => await SelectPage(1);

        public void OnCloseClicked() => _modalService.Cancel();

        // Payload format written by the Client Site Config page is "{userName}.{clientGuid}";
        // the GUID contains no dots, so the last segment is always the client id.
        public string GetClientName(string payload)
        {
            var clientGuid = payload?.Split('.').LastOrDefault();

            if (string.IsNullOrEmpty(clientGuid))
                return "-";

            var client = _clients.FirstOrDefault(c => string.Equals(c.ClientId.ToString(), clientGuid, StringComparison.OrdinalIgnoreCase));

            return client != null ? $"{client.ClientName} ({client.ClientCode})" : clientGuid;
        }

        // Extracts only the settings that differ between the From and To config snapshots so the
        // table shows "Dark Mode: false -> true" instead of two full JSON blobs. Returns null when
        // either side is not a config JSON payload, in which case the caller falls back to raw text.
        public static List<ClientSiteConfigLogChange> GetChanges(string fromJson, string toJson)
        {
            try
            {
                var from = JsonSerializer.Deserialize<ClientFeatureFlagsResponse>(fromJson);
                var to = JsonSerializer.Deserialize<ClientFeatureFlagsResponse>(toJson);

                if (from?.Features == null || to?.Features == null)
                    return null;

                var changes = new List<ClientSiteConfigLogChange>();

                void AddIfChanged(string setting, string fromValue, string toValue)
                {
                    if (fromValue != toValue)
                        changes.Add(new ClientSiteConfigLogChange() { Setting = setting, From = fromValue, To = toValue });
                }

                AddIfChanged("Betting Rules", from.Features.BettingRules.ToString(), to.Features.BettingRules.ToString());
                AddIfChanged("Dark Mode", from.Features.DarkMode.ToString(), to.Features.DarkMode.ToString());
                AddIfChanged("Language Options", from.Features.LanguageOptions.ToString(), to.Features.LanguageOptions.ToString());
                AddIfChanged("OG Auth", from.Features.OGAuth.ToString(), to.Features.OGAuth.ToString());
                AddIfChanged("Full Cashout", from.Features.FullCashout.ToString(), to.Features.FullCashout.ToString());
                AddIfChanged("Partial Cashout", from.Features.PartialCashout.ToString(), to.Features.PartialCashout.ToString());
                AddIfChanged("Bet Recommendation", from.Features.BetRecommendation.ToString(), to.Features.BetRecommendation.ToString());
                AddIfChanged("Euro Layout", from.Features.IsEuroLayout.ToString(), to.Features.IsEuroLayout.ToString());
                AddIfChanged("Game Tracker Product Names",
                    JoinOrDash(from.Features.GameTrackerProductNames),
                    JoinOrDash(to.Features.GameTrackerProductNames));

                var layoutKeys = (from.Layout?.Keys ?? Enumerable.Empty<string>())
                    .Union(to.Layout?.Keys ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

                foreach (var layoutKey in layoutKeys)
                {
                    ClientLayoutTheme fromTheme = null;
                    ClientLayoutTheme toTheme = null;
                    from.Layout?.TryGetValue(layoutKey, out fromTheme);
                    to.Layout?.TryGetValue(layoutKey, out toTheme);

                    AddIfChanged($"Themes ({layoutKey})", JoinOrDash(fromTheme?.Themes), JoinOrDash(toTheme?.Themes));
                }

                return changes;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string JoinOrDash(List<string> values) =>
            values != null && values.Any() ? string.Join(", ", values) : "-";
        #endregion

        #region Utilities
        // BO-104: the log row stores only the client GUID (in Payload), while the Client column
        // shows "Name (Code)". Resolve the search text to the GUIDs of matching clients so the API
        // can match them against Payload.
        private List<string> GetMatchingClientIds(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return new List<string>();

            return _clients
                .Where(c => (c.ClientName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
                         || (c.ClientCode?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(c => c.ClientId.ToString())
                .ToList();
        }

        private async Task GetContent()
        {
            if (!_isPreLoader)
            {
                _isPreLoader = true;
                StateHasChanged();
            }

            var response = await _backofficeService.GetActivityLogs(new ActivityLogRequest()
            {
                Page = AdminPermission.Admin_ClientSiteConfig.ToString().Replace("_", "."),
                Payload = "%",
                MetaData = _metaData,
                DateFrom = _dateFrom,
                DateTo = _dateTo,
                SearchText = _searchForm.SearchText,
                PayloadSearchTerms = GetMatchingClientIds(_searchForm.SearchText)
            });

            if (response.StatusCode == StatusCode.Success && response.Result != null)
            {
                _activityLogs = response.Result.ActivityLogs;
                _metaData = response.Result.MetaData;
            }
            else
            {
                _activityLogs = new List<ActivityLogModel>();
            }

            _isPreLoader = false;
        }
        #endregion
    }

    public class ClientSiteConfigLogChange
    {
        public string Setting { get; set; }
        public string From { get; set; }
        public string To { get; set; }
    }
}
