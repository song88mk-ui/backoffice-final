using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.LeagueSorting;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.LeagueSorting
{
    public partial class CopyModal
    {
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        private string _fromClientCode;
        private string _fromClientName;
        private int _sportId;
        private string _sportName;

        private List<LeagueSortingLeague> _leagues = new List<LeagueSortingLeague>();
        // Index 0 is the "all clients" entry that SelectWithCheckbox expects.
        private List<SelectListItemWithCheckboxModel<string>> _clients = new List<SelectListItemWithCheckboxModel<string>>();
        private List<(string Code, string Name)> _selected = new List<(string Code, string Name)>();

        private string _clientError;
        private bool _confirming;
        private bool _isSaving;
        private AlertModel _alert;

        #region Methods

        protected override void OnInitialized()
        {
            _fromClientCode = ModalParameters.TryGet<string>("FromClientCode");
            _fromClientName = ModalParameters.TryGet<string>("FromClientName");
            _sportId = ModalParameters.TryGet<int>("SportId");
            _sportName = ModalParameters.TryGet<string>("SportName");

            _leagues = (ModalParameters.TryGet<List<LeagueSortingLeague>>("Leagues") ?? new List<LeagueSortingLeague>())
                .OrderBy(l => l.SortOrder)
                .ToList();

            var targets = (_userState.Model.TradingClients ?? new List<PlatformClientResponse>())
                .Where(c => !string.Equals(c.ClientCode, _fromClientCode, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.ClientName)
                .Select(c => new SelectListItemWithCheckboxModel<string> { Value = c.ClientCode, Name = c.ClientName })
                .ToList();

            if (targets.Any())
                targets.Insert(0, new SelectListItemWithCheckboxModel<string> { Value = "all", Name = "All Clients" });

            _clients = targets;
        }

        // SelectWithCheckbox has already updated the checked state on the shared items when this runs.
        public void OnClientCheckboxEvent(Dictionary<string, string> items)
        {
            SyncSelected();
            _clientError = null;
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

        public void OnSaveClickEvent()
        {
            if (!_selected.Any())
            {
                _clientError = "Select client to proceed";
                return;
            }

            _clientError = null;
            _alert = new AlertModel();
            _confirming = true;
        }

        public void OnBackClickEvent()
        {
            _confirming = false;
        }

        public async Task OnConfirmClickEvent()
        {
            _alert = new AlertModel();
            _isSaving = true;

            var response = await _backofficeService.CopyClientOddstableLeagueSettings(new CopyClientOddstableLeagueSettingRequest
            {
                SourceClientCode = _fromClientCode,
                TargetClientCodes = _selected.Select(c => c.Code).ToList(),
                SportId = _sportId,
                UserId = _userState.Model.Id,
                UserName = _userState.Model.Name,
                TimeStamp = DateTime.UtcNow
            });

            _isSaving = false;

            if (response.StatusCode == StatusCode.Success && response.Result?.StatusCode == StatusCode.Success)
            {
                _alert = new AlertModel { Status = AlertStatus.Success, Message = "League Sorting copied!" };
                await InvokeAsync(StateHasChanged);

                await Modal.OnSaveClickCallback.InvokeAsync();
                await Task.Delay(1200);

                _modalService.Cancel();
                return;
            }

            _confirming = false;
            _alert = new AlertModel
            {
                Status = AlertStatus.Error,
                Message = response.Result?.Message ?? response.Message ?? "Error when copying league sorting."
            };
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

        #endregion
    }
}
