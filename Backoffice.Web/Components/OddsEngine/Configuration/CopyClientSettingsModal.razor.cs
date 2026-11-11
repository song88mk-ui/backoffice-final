using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    // Copy Client Settings: the league template assignments of one client replace those of the ticked clients.
    // BO-162: several target clients at once (same pattern as the League Sorting and Sport Settings copy dialogs), with
    // the confirmation step inside the dialog.
    public partial class CopyClientSettingsModal
    {
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        private EventCallback<AlertModel> OnCopyClientSettingsCallback { get; set; }

        private List<PlatformClientResponse> _fromClients = new List<PlatformClientResponse>();
        private string _fromClientCode;
        private string _fromClientName;

        // Index 0 is the "all clients" entry that SelectWithCheckbox expects. The From client is not listed.
        private List<SelectListItemWithCheckboxModel<string>> _clients = new List<SelectListItemWithCheckboxModel<string>>();
        private List<(string Code, string Name)> _selected = new List<(string Code, string Name)>();

        private bool _confirming;
        private bool _isSaving;
        private AlertModel _alert;

        private bool CanCopy => !string.IsNullOrEmpty(_fromClientCode) && _selected.Any();

        #region Methods

        protected override void OnInitialized()
        {
            OnCopyClientSettingsCallback = ModalParameters.TryGet<EventCallback<AlertModel>>("OnCopyClientSettingsCallback");

            _fromClients = (_userState.Model.TradingClients ?? new List<PlatformClientResponse>())
                .Where(c => !string.IsNullOrEmpty(c.ClientCode))
                .GroupBy(c => c.ClientCode, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(c => c.ClientName)
                .ToList();

            SetFromClient(_fromClients.FirstOrDefault()?.ClientCode);
        }

        public void OnFromClientChangedEvent(ChangeEventArgs e)
        {
            SetFromClient(e.Value?.ToString());
            _alert = null;
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

        public void OnCancelClickEvent()
        {
            _modalService.Cancel();
        }

        public void OnCopyClickEvent()
        {
            if (!CanCopy)
            {
                _alert = Error("Please select the clients you want to copy to.");
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

            var response = await _backofficeService.CopyClientConfig(new CopyClientConfigRequest
            {
                FromClientCode = _fromClientCode,
                ToClients = _selected
                    .Select(s => _fromClients.First(c => c.ClientCode == s.Code))
                    .Select(c => new CopyClientConfigTarget
                    {
                        ClientId = c.ClientId,
                        ClientCode = c.ClientCode,
                        ClientName = c.ClientName
                    })
                    .ToList(),
                UserId = _userState.Model.Id
            });

            _isSaving = false;

            var targets = response?.Result?.Targets ?? new List<CopyClientConfigTargetResult>();
            var copied = targets.Where(t => t.StatusCode == StatusCode.Success).ToList();
            var failed = targets.Where(t => t.StatusCode != StatusCode.Success).ToList();

            if (response?.StatusCode == StatusCode.Success)
            {
                _modalService.Cancel();
                await OnCopyClientSettingsCallback.InvokeAsync(new AlertModel
                {
                    Status = AlertStatus.Success,
                    Message = $"Client Settings copied to {Describe(copied)}.",
                    Opacity = 1.0m
                });
                return;
            }

            _confirming = false;

            if (copied.Any())
            {
                // Some clients were copied: say which, and which were not.
                _alert = Error($"Copied to {NamesOf(copied)}. Not copied to {Failures(failed)}.");
                return;
            }

            _alert = Error(failed.Any()
                ? $"Not copied to {Failures(failed)}."
                : response?.Message ?? "Copy Client Settings failed. Please try again.");
        }

        #endregion

        #region Utilities

        // The From client is never offered as a target. Ticks on the other clients are kept when From changes.
        private void SetFromClient(string clientCode)
        {
            var from = _fromClients.FirstOrDefault(c => string.Equals(c.ClientCode, clientCode, StringComparison.OrdinalIgnoreCase));

            _fromClientCode = from?.ClientCode;
            _fromClientName = from?.ClientName;

            var ticked = _clients.Where(c => c.Value != "all" && c.IsChecked).Select(c => c.Value).ToHashSet();

            var targets = _fromClients
                .Where(c => c.ClientCode != _fromClientCode)
                .Select(c => new SelectListItemWithCheckboxModel<string>
                {
                    Value = c.ClientCode,
                    Name = c.ClientName,
                    IsChecked = ticked.Contains(c.ClientCode)
                })
                .ToList();

            if (targets.Any())
            {
                targets.Insert(0, new SelectListItemWithCheckboxModel<string>
                {
                    Value = "all",
                    Name = "All Clients",
                    IsChecked = targets.All(t => t.IsChecked)
                });
            }

            _clients = targets;

            SyncSelected();
        }

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

        private string Describe(List<CopyClientConfigTargetResult> copied) =>
            copied.Count == 1 ? NameOf(copied[0]) : $"{copied.Count} clients";

        private string NameOf(CopyClientConfigTargetResult target) =>
            _fromClients.FirstOrDefault(c => c.ClientCode == target.ClientCode)?.ClientName ?? target.ClientName ?? target.ClientCode;

        private string NamesOf(IEnumerable<CopyClientConfigTargetResult> targets) => string.Join(", ", targets.Select(NameOf));

        private string Failures(IEnumerable<CopyClientConfigTargetResult> failed) =>
            string.Join(", ", failed.Select(t => $"{NameOf(t)} ({t.Message})"));

        #endregion
    }
}
