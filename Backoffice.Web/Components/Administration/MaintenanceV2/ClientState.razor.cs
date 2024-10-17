using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Administration.MaintenanceV2
{
    public partial class ClientState
    {
        [Parameter] public bool IsLive { get; set; }
        [Parameter] public bool IsCompleteClients { get; set; }
        [Parameter] public List<string> Clients { get; set; }

        private string _headerText = "";
        private string _stateText = "";
        private string _stateCss = "";

        private bool _isShowClient;

        protected override void OnParametersSet()
        {
            _headerText = IsCompleteClients ? "All client sites are" : "The following client sites are";
            _isShowClient = (Clients?.Any() ?? false) && !IsCompleteClients;

            if (IsLive)
            {
                _stateText = "LIVE";
                _stateCss = "live";
            }
            else
            {
                _stateText = "UNDER MAINTENANCE";
                _stateCss = "maintenance";
            }
        }
    }
}
