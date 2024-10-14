using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Backoffice.Web.Components.Administration.MaintenanceV2
{
    public partial class ClientSelectionDropDown
    {
        [Parameter] public bool Disable { get; set; }
        [Parameter] public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }

        private string _searchField = "";
        private string _selectionCss;
        private List<SelectListItemWithCheckboxModel<string>> _activeClients = new();
        private bool _init = true;
        private bool _shouldRender = true;

        protected override void OnInitialized()
        {
        }
        protected override void OnParametersSet()
        {
            if (Clients != null && !_activeClients.SequenceEqual(Clients.Where(c => c.IsChecked).ToList()))
            {
                _activeClients = Clients.Where(c => c.IsChecked).ToList();
                _shouldRender = true;
            }
        }
        protected override bool ShouldRender() => _shouldRender;
        protected override void OnAfterRender(bool firstRender) => _shouldRender = false;
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
            _shouldRender = true;

            ReOrderClients();
        }
        private async Task HandleCheckboxCheck(SelectListItemWithCheckboxModel<string> client)
        {
            var isChecked = !client.IsChecked;

            if (isChecked)
            {
                _activeClients.Add(new SelectListItemWithCheckboxModel<string>()
                {
                    Value = client.Value,
                    Name = client.Name,
                    IsChecked = isChecked
                });

                ReOrderClients();
            }
            else
            {
                await HandleRemoveClientClick(client.Value);
            }

        }
        private void ReOrderClients() => Clients = Clients.Where(x => x.Name.Contains(_searchField, StringComparison.OrdinalIgnoreCase) || x.Value.Contains(_searchField, StringComparison.OrdinalIgnoreCase)).OrderByDescending(c => c.IsChecked).ToList();
    }
}
