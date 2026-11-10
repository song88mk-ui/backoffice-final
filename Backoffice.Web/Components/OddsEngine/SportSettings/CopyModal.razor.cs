using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    // BO-160: copies the Sport Settings shown on screen for one client to one or more other clients.
    // Same pattern as the League Sorting and Market Sort Order copy dialogs.
    public partial class CopyModal
    {
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        private string _fromClientCode;
        private string _fromClientName;
        private List<SportsSortOrder> _sports = new List<SportsSortOrder>();

        // Index 0 is the "all clients" entry that SelectWithCheckbox expects. The source client is not listed.
        private List<SelectListItemWithCheckboxModel<string>> _clients = new List<SelectListItemWithCheckboxModel<string>>();
        private List<(string Code, string Name)> _selected = new List<(string Code, string Name)>();
        private Dictionary<string, PlatformClientResponse> _clientsByCode = new Dictionary<string, PlatformClientResponse>(StringComparer.OrdinalIgnoreCase);

        private bool _confirming;
        private bool _isSaving;
        private AlertModel _alert;

        private bool CanCopy => _selected.Any() && _sports.Any();

        #region Methods

        protected override void OnInitialized()
        {
            _fromClientCode = ModalParameters.TryGet<string>("FromClientCode");
            _sports = ModalParameters.TryGet<List<SportsSortOrder>>("Sports") ?? new List<SportsSortOrder>();

            var tradingClients = _userState.Model.TradingClients ?? new List<PlatformClientResponse>();

            _clientsByCode = tradingClients
                .Where(c => !string.IsNullOrEmpty(c.ClientCode))
                .GroupBy(c => c.ClientCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            _fromClientName = _clientsByCode.TryGetValue(_fromClientCode ?? string.Empty, out var fromClient)
                ? fromClient.ClientName
                : _fromClientCode;

            var targets = _clientsByCode.Values
                .Where(c => !string.Equals(c.ClientCode, _fromClientCode, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.ClientName)
                .Select(c => new SelectListItemWithCheckboxModel<string>
                {
                    Value = c.ClientCode,
                    Name = c.ClientName
                })
                .ToList();

            if (targets.Any())
            {
                targets.Insert(0, new SelectListItemWithCheckboxModel<string>
                {
                    Value = "all",
                    Name = "All Clients"
                });
            }

            _clients = targets;

            // Copying an empty list would wipe every target's sports.
            if (!_sports.Any())
                _alert = Error($"{_fromClientName} has no sports to copy.");
        }

        // SelectWithCheckbox has already updated the checked state on the shared items when this runs.
        public void OnClientCheckboxEvent(Dictionary<string, string> items)
        {
            SyncSelected();

            if (_sports.Any())
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

        public void OnCancelClickEvent()
        {
            _modalService.Cancel();
        }

        public void OnCopyClickEvent()
        {
            if (!CanCopy)
            {
                _alert = Error(_sports.Any()
                    ? "Please select the clients you want to copy to."
                    : $"{_fromClientName} has no sports to copy.");
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

            var response = await _backofficeService.CopyClientSportSettings(new CopyClientSportSettingsRequest
            {
                FromClientCode = _fromClientCode,
                ToClientCodes = _selected.Select(c => c.Code).ToList(),
                SportsSortOrder = _sports,
                UserId = _userState.Model.Id,
                UserName = _userState.Model.Name
            });

            _isSaving = false;

            var targets = response?.Result?.Targets ?? new List<CopyClientSportSettingsTargetResult>();
            var copied = targets.Where(t => t.StatusCode == StatusCode.Success).ToList();
            var failed = targets.Where(t => t.StatusCode != StatusCode.Success).ToList();

            if (response?.StatusCode == StatusCode.Success)
            {
                var warnings = Warnings(copied);

                _alert = new AlertModel
                {
                    Status = AlertStatus.Success,
                    Message = $"Sport Settings copied to {Describe(copied.Count)}!" + warnings,
                    IsAlwaysShown = warnings.Length > 0
                };

                // Keep the dialog open when a cache refresh or log failed, so the warning is read.
                if (warnings.Length > 0)
                {
                    _confirming = false;
                    return;
                }

                await InvokeAsync(StateHasChanged);
                await Task.Delay(1200);

                _modalService.Cancel();
                return;
            }

            _confirming = false;

            if (copied.Any())
            {
                // Some clients were copied: say which, and which were not.
                _alert = Error($"Copied to {NamesOf(copied)}.{Warnings(copied)} Not copied to {string.Join(", ", failed.Select(t => $"{NameOf(t)} ({t.Message})"))}.");
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

        private static AlertModel Error(string message) => new AlertModel
        {
            Status = AlertStatus.Error,
            Message = message,
            IsAlwaysShown = true
        };

        private string Describe(int count) => count == 1 && _selected.Count == 1 ? _selected[0].Name : $"{count} clients";

        private string NameOf(CopyClientSportSettingsTargetResult target) =>
            _clientsByCode.TryGetValue(target.ClientCode ?? string.Empty, out var client) ? client.ClientName : target.ClientCode;

        private string NamesOf(IEnumerable<CopyClientSportSettingsTargetResult> targets) => string.Join(", ", targets.Select(NameOf));

        // A copied client whose cache refresh or activity log failed carries a warning instead of "Copied".
        private string Warnings(IEnumerable<CopyClientSportSettingsTargetResult> copied)
        {
            var warned = copied.Where(t => t.Message != "Copied").ToList();
            return warned.Any() ? $" {NamesOf(warned)}: {warned[0].Message}" : string.Empty;
        }

        #endregion
    }
}
