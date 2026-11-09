using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MarketSortOrder
{
    // BO-159: copies the on-screen client's market sort order and market groups to one or more clients,
    // all with the same sport and scope. Same pattern as the League Sorting copy dialog.
    public partial class CopyModal
    {
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        private string _fromClientCode;
        private string _fromClientName;
        private int _fromSportId;
        private string _fromSportName;
        private int _fromFixtureStatusId;
        private string _fromFixtureStatusName;

        // Index 0 is the "all clients" entry that SelectWithCheckbox expects. The source client stays in the list:
        // it can be a target for another sport or scope.
        private List<SelectListItemWithCheckboxModel<string>> _clients = new List<SelectListItemWithCheckboxModel<string>>();
        private List<(string Code, string Name)> _selected = new List<(string Code, string Name)>();
        private Dictionary<string, PlatformClientResponse> _clientsByCode = new Dictionary<string, PlatformClientResponse>(StringComparer.OrdinalIgnoreCase);

        private List<SportModel> _sports = new List<SportModel>();
        private int _selectedSportId;

        private List<SelectListItemModel<int>> _scopes = new List<SelectListItemModel<int>>();
        private int _selectedScopeId = -1;

        private bool _confirming;
        private bool _isSaving;
        private AlertModel _alert;

        private bool CanCopy => _selected.Any() && _selectedSportId > 0 && _selectedScopeId >= 0;
        private string SelectedSportName => _sports.FirstOrDefault(s => s.SportId == _selectedSportId)?.SportName;
        private string SelectedScopeName => _scopes.FirstOrDefault(s => s.Value == _selectedScopeId)?.Name;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _fromClientCode = ModalParameters.TryGet<string>("FromClientCode");
            _fromSportId = ModalParameters.TryGet<int>("FromSportId");
            _fromSportName = ModalParameters.TryGet<string>("FromSportName");
            _fromFixtureStatusId = ModalParameters.TryGet<int>("FromFixtureStatusId");
            _fromFixtureStatusName = ModalParameters.TryGet<string>("FromFixtureStatusName");

            var tradingClients = _userState.Model.TradingClients ?? new List<PlatformClientResponse>();

            _clientsByCode = tradingClients
                .Where(c => !string.IsNullOrEmpty(c.ClientCode))
                .GroupBy(c => c.ClientCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            _fromClientName = _clientsByCode.TryGetValue(_fromClientCode ?? string.Empty, out var fromClient)
                ? fromClient.ClientName
                : _fromClientCode;

            var targets = _clientsByCode.Values
                .OrderBy(c => c.ClientName)
                .Select(c => new SelectListItemWithCheckboxModel<string> { Value = c.ClientCode, Name = c.ClientName })
                .ToList();

            if (targets.Any())
                targets.Insert(0, new SelectListItemWithCheckboxModel<string> { Value = "all", Name = "All Clients" });

            _clients = targets;

            // The same lookup as the Market Sort Order filter, so both sport lists match.
            try
            {
                var sportsResponse = await _backofficeService.GetSportLookup(_translationState.ActiveLanguageCode.ToString());

                if (sportsResponse?.Result?.SportLookup != null)
                {
                    _sports = sportsResponse.Result.SportLookup
                        .Select(s => new SportModel { SportId = s.SportId, SportName = s.SportName })
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Market Sort Order copy dialog could not load the sports");
            }

            _scopes = Enum.GetValues(typeof(Scope))
                .Cast<Scope>()
                .Select(s => new SelectListItemModel<int> { Value = (int)s, Name = s.ToString() })
                .ToList();
        }

        // SelectWithCheckbox has already updated the checked state on the shared items when this runs.
        public void OnClientCheckboxEvent(Dictionary<string, string> items)
        {
            SyncSelected();
            _alert = null;
        }

        public void OnRemoveClientEvent(string clientCode)
        {
            var item = _clients.FirstOrDefault(c => c.Value == clientCode);

            if (item != null)
                item.IsChecked = false;

            if (_clients.Any())
                _clients[0].IsChecked = false;

            SyncSelected();
        }

        public void OnSportChangedEvent(ChangeEventArgs e)
        {
            _selectedSportId = int.TryParse(e.Value?.ToString(), out var sportId) ? sportId : 0;
            _alert = null;
        }

        public void OnScopeChangedEvent(ChangeEventArgs e)
        {
            _selectedScopeId = int.TryParse(e.Value?.ToString(), out var scopeId) ? scopeId : -1;
            _alert = null;
        }

        public void OnCancelClickEvent()
        {
            _modalService.Cancel();
        }

        public void OnCopyClickEvent()
        {
            if (!CanCopy)
            {
                _alert = Error("Please select the clients, sport, and scope you want to copy to.");
                return;
            }

            // The source client is a valid target only for another sport or scope.
            if (_selected.Any(c => string.Equals(c.Code, _fromClientCode, StringComparison.OrdinalIgnoreCase))
                && _selectedSportId == _fromSportId
                && _selectedScopeId == _fromFixtureStatusId)
            {
                _alert = Error($"You're copying {_fromClientName} to the same client, sport, and scope. Remove it or change the sport or scope before copying.");
                return;
            }

            _alert = null;
            _confirming = true;
        }

        public void OnBackClickEvent()
        {
            _confirming = false;
        }

        public async Task OnConfirmClickEvent()
        {
            _alert = null;
            _isSaving = true;

            var response = await _backofficeService.CopyClientMarketSortOrder(new CopyClientMarketSortOrderRequest
            {
                CopyFromClientId = _clientsByCode.TryGetValue(_fromClientCode ?? string.Empty, out var fromClient) ? fromClient.ClientId : Guid.Empty,
                CopyFromSportId = _fromSportId,
                CopyFromFixtureStatusId = _fromFixtureStatusId,
                CopyToClientIds = _selected.Select(c => _clientsByCode[c.Code].ClientId).ToList(),
                CopyToSportId = _selectedSportId,
                CopyToFixtureStatusId = _selectedScopeId,
                UserId = _userState.Model.Id,
                UserName = _userState.Model.Name
            });

            _isSaving = false;

            var targets = response?.Result?.Targets ?? new List<CopyClientMarketSortOrderTargetResult>();
            var copied = targets.Where(t => t.StatusCode == StatusCode.Success).ToList();
            var failed = targets.Where(t => t.StatusCode != StatusCode.Success).ToList();

            if (response?.StatusCode == StatusCode.Success)
            {
                var warnings = Warnings(copied);

                _alert = new AlertModel
                {
                    Status = AlertStatus.Success,
                    Message = $"Market Sort Order copied to {Describe(copied.Count)}!" + warnings,
                    IsAlwaysShown = warnings.Length > 0
                };
                await InvokeAsync(StateHasChanged);

                await Modal.OnSaveClickCallback.InvokeAsync();

                // Keep the dialog open when a cache refresh failed, so the warning is read.
                if (warnings.Length > 0)
                {
                    _confirming = false;
                    return;
                }

                await Task.Delay(1200);

                _modalService.Cancel();
                return;
            }

            _confirming = false;

            if (copied.Any())
            {
                // Some clients were copied: say which, and which were not, and refresh the page behind the dialog.
                _alert = Error($"Copied to {NamesOf(copied)}. Not copied to {string.Join(", ", failed.Select(t => $"{NameOf(t)} ({t.Message})"))}.");
                await Modal.OnSaveClickCallback.InvokeAsync();
                return;
            }

            _alert = Error(failed.Any()
                ? $"Not copied to {string.Join(", ", failed.Select(t => $"{NameOf(t)} ({t.Message})"))}."
                : response?.Message ?? "Copy failed. Please try again.");
        }

        #endregion

        #region Utilities

        private void SyncSelected()
        {
            _selected = _clients
                .Where(c => c.Value != "all" && c.IsChecked)
                .Select(c => (c.Value, c.Name))
                .ToList();
        }

        private static AlertModel Error(string message) => new AlertModel { Status = AlertStatus.Error, Message = message, IsAlwaysShown = true };

        private string Describe(int count) => count == 1 && _selected.Count == 1 ? _selected[0].Name : $"{count} clients";

        private string NameOf(CopyClientMarketSortOrderTargetResult target) =>
            _clientsByCode.Values.FirstOrDefault(c => c.ClientId == target.ClientId)?.ClientName ?? target.ClientCode ?? target.ClientId.ToString();

        private string NamesOf(IEnumerable<CopyClientMarketSortOrderTargetResult> targets) => string.Join(", ", targets.Select(NameOf));

        // A copied client whose cache message failed carries a warning instead of "Copied".
        private string Warnings(IEnumerable<CopyClientMarketSortOrderTargetResult> copied)
        {
            var warned = copied.Where(t => t.Message != "Copied").ToList();
            return warned.Any() ? $" {NamesOf(warned)}: {warned[0].Message}" : string.Empty;
        }

        #endregion
    }
}
