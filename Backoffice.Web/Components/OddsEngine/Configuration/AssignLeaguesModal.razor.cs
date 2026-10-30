using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Serilog;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public enum AssignLeaguesMode
    {
        Assign,
        ChangeState,
        Unassign
    }

    // BO-151: assign the selected leagues to their clients as Active or Restricted, change that state, or unassign them.
    // The user has to choose a state: nothing is pre-selected, so a league is never restricted by a stray click on Apply.
    // The API saves the change and writes the activity log, so this dialog only sends the request.
    public partial class AssignLeaguesModal
    {
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        private List<ConfigClientViewModel> _leagues = new();
        private AssignLeaguesMode _mode = AssignLeaguesMode.Assign;
        private bool? _restricted;
        private bool _isSaving;
        private bool _showAll;
        private AlertModel _alertModel = new();

        private bool CanSave => !_isSaving && _leagues.Count > 0 && (_mode == AssignLeaguesMode.Unassign || _restricted.HasValue);

        protected override void OnInitialized()
        {
            _leagues = ModalParameters?.TryGet<List<ConfigClientViewModel>>("Leagues") ?? new List<ConfigClientViewModel>();
            _mode = ModalParameters?.TryGet<AssignLeaguesMode>("Mode") ?? AssignLeaguesMode.Assign;
        }

        private void HandleActiveSelected() => _restricted = false;

        private void HandleRestrictedSelected() => _restricted = true;

        private void HandleShowAllClick() => _showAll = !_showAll;

        public void HandleCancelClick() => _modalService.Cancel();

        public async Task HandleSaveClick()
        {
            if (!CanSave)
                return;

            _isSaving = true;
            _alertModel = new AlertModel();

            var request = new ConfigSetLeagueStateRequest
            {
                Restricted = _restricted == true,
                Leagues = _leagues.Select(x => new ConfigClientModel
                {
                    Id = x.Id,
                    ClientId = x.ClientId,
                    ClientCode = x.ClientCode,
                    ClientName = x.ClientName,
                    SportId = x.SportId,
                    SportName = x.SportName,
                    SegmentId = x.SegmentId,
                    SegmentName = x.SegmentName,
                    LeagueId = x.LeagueId,
                    LeagueName = x.LeagueName
                }).ToList()
            };

            var response = await _backofficeService.SetLeagueState(_mode == AssignLeaguesMode.Unassign ? ConfigState.UnAssigned : ConfigState.Assigned, request);

            if (response == null || response.StatusCode != StatusCode.Success)
            {
                // The API message goes to the log, not to the screen. The dialog stays open so the user can try again.
                Log.Warning("OE Config could not save {Mode} for {LeagueCount} leagues: {Message}", _mode, _leagues.Count, response?.Message);

                _alertModel = new AlertModel
                {
                    Status = AlertStatus.Error,
                    Message = "The leagues could not be saved. Please try again, or contact support if it keeps happening.",
                    IsAlwaysShown = true
                };
                _isSaving = false;
                return;
            }

            await Modal.OnSaveClickCallback.InvokeAsync(new AlertModel
            {
                Status = AlertStatus.Success,
                Message = SuccessMessage()
            });

            _modalService.Cancel();
        }

        private string SuccessMessage()
        {
            var state = _restricted == true ? "Restricted" : "Active";

            return _mode switch
            {
                AssignLeaguesMode.Unassign => "The selected leagues were unassigned.",
                AssignLeaguesMode.ChangeState => $"The state of the selected leagues was changed to {state}.",
                _ => $"The selected leagues were assigned as {state}."
            };
        }
    }
}
