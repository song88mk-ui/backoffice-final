using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.MarketGroups
{
    // BO-158: copies the market groups of the client, sport and scope on screen to the clients ticked here,
    // with a confirmation step before the targets' groups are replaced (the same flow as League Sorting Copy).
    public partial class CopyMarketGroupModal
    {
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        private Guid _fromClientId;
        private string _fromClientCode;
        private string _fromClientName;
        private int _sportId;
        private string _sportName;
        private int _fixtureStatusId;
        private string _scopeName;

        private List<ClientMarketTemplateGroupDTO> _groups = new List<ClientMarketTemplateGroupDTO>();
        // Index 0 is the "all clients" entry that SelectWithCheckbox expects.
        private List<SelectListItemWithCheckboxModel<string>> _clients = new List<SelectListItemWithCheckboxModel<string>>();
        private Dictionary<string, PlatformClientResponse> _clientsByCode = new Dictionary<string, PlatformClientResponse>();
        private List<(Guid Id, string Code, string Name)> _selected = new List<(Guid Id, string Code, string Name)>();

        private string _clientError;
        private bool _confirming;
        private bool _isSaving;
        private AlertModel _alert;

        #region Methods

        protected override void OnInitialized()
        {
            _fromClientId = ModalParameters.TryGet<Guid>("ClientId");
            _fromClientCode = ModalParameters.TryGet<string>("ClientCode");
            _sportId = ModalParameters.TryGet<int>("SportId");
            _fixtureStatusId = ModalParameters.TryGet<int>("FixtureStatusId");
            _scopeName = ((FixtureStatus)_fixtureStatusId).ToString();

            _groups = (ModalParameters.TryGet<List<ClientMarketTemplateGroupDTO>>("Groups") ?? new List<ClientMarketTemplateGroupDTO>())
                .OrderBy(g => g.SortOrder)
                .ToList();

            var tradingClients = _userState.Model.TradingClients ?? new List<PlatformClientResponse>();

            _fromClientName = tradingClients.FirstOrDefault(c => string.Equals(c.ClientCode, _fromClientCode, StringComparison.OrdinalIgnoreCase))?.ClientName ?? _fromClientCode;

            var sportName = ModalParameters.TryGet<string>("SportName");
            _sportName = string.IsNullOrEmpty(sportName) ? $"Sport {_sportId}" : _translationState.GetValue(sportName);

            var targetClients = tradingClients
                .Where(c => !string.Equals(c.ClientCode, _fromClientCode, StringComparison.OrdinalIgnoreCase) && c.ClientId != _fromClientId)
                .GroupBy(c => c.ClientCode)
                .Select(g => g.First())
                .OrderBy(c => c.ClientName)
                .ToList();

            _clientsByCode = targetClients.ToDictionary(c => c.ClientCode);

            var targets = targetClients
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
            if (!_groups.Any())
                return;

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

            var targets = _selected.ToList();

            var response = await _backofficeService.CopyClientMarketTemplateGroup(new ClientCopyMarketTemplateGroupRequest
            {
                ClientId = _fromClientId,
                SportId = _sportId,
                FixtureStatusId = _fixtureStatusId,
                CopyToClientIds = targets.Select(t => t.Id).ToList(),
                CopyToSportId = _sportId,
                CopyToFixtureStatusId = _fixtureStatusId
            });

            var results = response?.Result?.Targets ?? new List<ClientCopyMarketTemplateGroupTargetResult>();
            var copied = new List<(Guid Id, string Code, string Name)>();
            var notCopied = new List<string>();

            foreach (var target in targets)
            {
                var result = results.FirstOrDefault(r => r.ClientId == target.Id);

                if (result != null && result.StatusCode == StatusCode.Success)
                {
                    copied.Add(target);
                    await WriteActivityLog(target.Code, result);
                }
                else
                {
                    var reason = result?.Message ?? response?.Message;
                    notCopied.Add(string.IsNullOrEmpty(reason) ? target.Name : $"{target.Name} ({reason})");
                }
            }

            if (!notCopied.Any())
            {
                _alert = new AlertModel { Status = AlertStatus.Success, Message = $"Market Groups copied to {string.Join(", ", copied.Select(c => c.Name))}!" };
                await InvokeAsync(StateHasChanged);

                await Modal.OnSaveClickCallback.InvokeAsync(true);
                await Task.Delay(1200);

                _isSaving = false;
                _modalService.Cancel();
                return;
            }

            // Leave only the clients that were not copied ticked, so Save tries just those again.
            foreach (var client in copied)
            {
                var item = _clients.FirstOrDefault(c => c.Value == client.Code);

                if (item != null)
                    item.IsChecked = false;
            }

            if (copied.Any() && _clients.Any())
                _clients[0].IsChecked = false;

            SyncSelected();

            _isSaving = false;
            _confirming = false;
            _alert = new AlertModel
            {
                Status = AlertStatus.Error,
                Message = copied.Any()
                    ? $"Copied to: {string.Join(", ", copied.Select(c => c.Name))}. Not copied: {string.Join(", ", notCopied)}."
                    : $"Not copied: {string.Join(", ", notCopied)}."
            };
        }

        #endregion

        #region Utilities

        private void SyncSelected()
        {
            _selected = _clients
                .Where(c => c.Value != "all" && c.IsChecked && _clientsByCode.ContainsKey(c.Value))
                .Select(c => (_clientsByCode[c.Value].ClientId, c.Value, c.Name))
                .ToList();
        }

        // One Copy entry per copied client, in that client's own log (the payload starts with its code).
        private async Task WriteActivityLog(string clientCode, ClientCopyMarketTemplateGroupTargetResult result)
        {
            await _backofficeService.AddActivityLog(new ActivityLogModel
            {
                Page = SportsbookClientPermission.SBC_OE_MarketSetting_Group.ToString().Replace("_", "."),
                Payload = $"{clientCode}.{_sportId}.{_fixtureStatusId}.0",
                Activity = Activity.Copy.ToString(),
                From = JsonSerializer.Serialize(ToActivityModels(result.PreviousGroups)),
                To = JsonSerializer.Serialize(ToActivityModels(result.CopiedGroups)),
                UserId = _userState.Model.Id,
                UserName = _userState.Model.Name
            });
        }

        private static List<MarketGroupActivtyModel> ToActivityModels(List<ClientMarketTemplateGroupDTO> groups) =>
            (groups ?? new List<ClientMarketTemplateGroupDTO>())
                .Select(g => new MarketGroupActivtyModel
                {
                    MarketTemplateGroupId = g.MarketTemplateGroupId,
                    Name = g.Name,
                    SortOrder = g.SortOrder,
                    MarketTemplateIds = g.MarketTemplateIds,
                    IsActive = g.IsActive
                })
                .ToList();

        #endregion
    }
}
