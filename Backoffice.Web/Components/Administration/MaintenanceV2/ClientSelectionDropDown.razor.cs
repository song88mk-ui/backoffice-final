using Backoffice.Web.Models;
using Backoffice.Web.Pages.SBC;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Identity.Client;
using static System.Net.Mime.MediaTypeNames;

namespace Backoffice.Web.Components.Administration.MaintenanceV2
{
    public partial class ClientSelectionDropDown
    {
        [Parameter] public bool Disable { get; set; }
        [Parameter] public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        [Parameter] public EventCallback<List<SelectListItemWithCheckboxModel<string>>> ClientsChanged { get; set; }
        [Parameter] public EventCallback<bool> OnHasChange { get; set; }

        private string _searchField = "";
        private string _selectionCss;
        private List<SelectListItemWithCheckboxModel<string>> _activeClients = new();
        private bool _init = true;
        private bool _shouldRender = true;
        private List<string> _initialClients = new();
        private bool _hasChange;
        private bool _isDisabled;

        protected override async Task OnInitializedAsync()
        {
            var clientConfigs = await _backofficeService.GetMaintenance();
        }
        protected override void OnParametersSet()
        {
            if (ParameterChanged())
            {
                Clients = Clients.Where(x => x.Name.Contains(_searchField, StringComparison.OrdinalIgnoreCase) || x.Value.Contains(_searchField, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(c => c.IsChecked).ToList();

                _activeClients = Clients.Where(c => c.IsChecked).ToList();
                _isDisabled = Disable;
                _shouldRender = true;

                if (_init)
                    _initialClients = new(_activeClients.Select(s => s.Value));
            }
        }
        protected override bool ShouldRender() => _shouldRender;
        protected override void OnAfterRender(bool firstRender) => _shouldRender = false;
        private bool ParameterChanged() => Clients != null && !_activeClients.SequenceEqual(Clients.Where(c => c.IsChecked).ToList()) || _isDisabled != Disable;
        private void HandleEditListClick()
        {
            //_selectionCss = " bg-white";
        }

        public async Task HandleSearchInput()
        {
        }
        private async Task HandleRemoveClientClick(string clientId)
        {
            var client = Clients.FirstOrDefault(c => c.Value == clientId);
            if (client != null)
                client.IsChecked = false;


            var activeClient = _activeClients.FirstOrDefault(c => c.Value == clientId);
            if (activeClient == null)
                return;

            _activeClients.Remove(activeClient);

            await ClientsChanged.InvokeAsync(Clients);

            _shouldRender = true;
        }
        private async Task HandleCheckboxCheck(SelectListItemWithCheckboxModel<string> client)
        {
            client.IsChecked = !client.IsChecked;

            if (client.IsChecked)
            {
                if (!_activeClients.Any(c => c.Value == client.Value))
                {
                    _activeClients.Add(new SelectListItemWithCheckboxModel<string>()
                    {
                        Value = client.Value,
                        Name = client.Name,
                        IsChecked = client.IsChecked
                    });

                    await ClientsChanged.InvokeAsync(Clients);
                    _shouldRender = true;
                }
            }
            else
            {
                await HandleRemoveClientClick(client.Value);
            }

            HandleChangeValue();
        }
        private void ReOrderClients()
        {
            Clients = Clients.Where(x => x.Name.Contains(_searchField, StringComparison.OrdinalIgnoreCase) || x.Value.Contains(_searchField, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.IsChecked).ToList();

            HandleChangeValue();
        }
        private async void HandleChangeValue()
        {
            var value = false;
            var activeClients = _activeClients.Select(s => s.Value).ToList();

            if (!new HashSet<string>(activeClients).SetEquals(_initialClients))
            {
                value = true;
            }
            else if (_hasChange == true)
            {
                await OnHasChange.InvokeAsync(value);
            }

            if (value != _hasChange)
            {
                _hasChange = value;
                await OnHasChange.InvokeAsync(value);
            }
        }
    }
}
