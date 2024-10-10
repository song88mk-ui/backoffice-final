using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Backoffice.Web.Components.Administration.MaintenanceV2
{
    public partial class ClientSelectionDropDown
    {
        [Parameter] public bool Disable { get; set; }
        [Parameter] public List<string> Clients { get; set; }

        private string _searchField = "";
        private string _selectionCss;
        private List<string> _activeClients;
        private bool _init = true;
        private bool _shouldRender = true;

        protected override void OnInitialized()
        {
            Clients = new List<string>()
            {
                "Alpha88", "EKings Thailand", "Khelraja", "WSGaming" , "W88"
            };
            _activeClients = new(Clients);
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
        private async Task HandleRemoveClientClick(string client)
        {
            _activeClients.Remove(client);
            _shouldRender = true;
        }
    }
}
