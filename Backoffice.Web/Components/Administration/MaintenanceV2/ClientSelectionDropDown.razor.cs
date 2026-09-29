using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Administration.MaintenanceV2
{
    public partial class ClientSelectionDropDown
    {
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public List<MaintenanceSelectListItemWithCheckboxModel> Clients { get; set; }
        [Parameter] public EventCallback<List<MaintenanceSelectListItemWithCheckboxModel>> ClientsChanged { get; set; }
        [Parameter] public EventCallback<bool> OnHasChange { get; set; }

        private string _searchField = "";
        private string _selectionCss;
        private List<MaintenanceSelectListItemWithCheckboxModel> _activeClients = new();
        private bool _init = true;
        private bool _shouldRender = true;
        private List<string> _initialClients = new();
        private bool _hasChange;
        private bool _isDisabled;
        private bool _isDropdownOpen = false;

        protected override async Task OnInitializedAsync()
        {
        }
        protected override void OnParametersSet()
        {
            if (ParameterChanged())
            {
                _activeClients = Clients?.Where(c => c.IsChecked).ToList() ?? new();
                _isDisabled = Disabled;
                _shouldRender = true;

                if (_init)
                    _initialClients = new(Clients?.Where(c => c.IsMaintenanceEnabled).Select(s => s.Value) ?? Enumerable.Empty<string>());
            }

            _init = false;

        }
        protected override bool ShouldRender() => _shouldRender;
        protected override void OnAfterRender(bool firstRender) => _shouldRender = false;
        private bool ParameterChanged() => Clients != null && !_activeClients.SequenceEqual(Clients.Where(c => c.IsChecked).ToList()) || _isDisabled != Disabled;
        private void HandleEditListClick()
        {
            if (Disabled)
                return;

            _isDropdownOpen = !_isDropdownOpen;
            _shouldRender = true;
        }

        private void CloseDropdown()
        {
            _isDropdownOpen = false;
            _shouldRender = true;
        }

        public void HandleSearchInput(ChangeEventArgs e)
        {
            _searchField = e.Value?.ToString() ?? string.Empty;
            _shouldRender = true;
        }

        private List<MaintenanceSelectListItemWithCheckboxModel> FilteredClients() =>
            Clients?.Where(x => x.Name.Contains(_searchField, StringComparison.OrdinalIgnoreCase) || x.Value.Contains(_searchField, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.IsChecked).ToList() ?? new();
        private async Task HandleRemoveClientClick(string clientId)
        {
            if (Disabled)
                return;

            var client = Clients.FirstOrDefault(c => c.Value == clientId);
            if (client != null)
                client.IsChecked = false;


            var activeClient = _activeClients.FirstOrDefault(c => c.Value == clientId);
            if (activeClient == null)
                return;

            _activeClients.Remove(activeClient);

            HandleChangeValue();
            await ClientsChanged.InvokeAsync(Clients);

            _shouldRender = true;
        }
        private async Task HandleCheckboxCheck(MaintenanceSelectListItemWithCheckboxModel client)
        {
            client.IsChecked = !client.IsChecked;

            if (client.IsChecked)
            {
                if (!_activeClients.Any(c => c.Value == client.Value))
                {
                    _activeClients.Add(new MaintenanceSelectListItemWithCheckboxModel()
                    {
                        Value = client.Value,
                        Name = client.Name,
                        IsChecked = client.IsChecked,
                        IsMaintenanceEnabled = client.IsMaintenanceEnabled
                    });

                    await ClientsChanged.InvokeAsync(Clients);
                    _shouldRender = true;
                }

                HandleChangeValue();
            }
            else
            {
                await HandleRemoveClientClick(client.Value);
            }

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
