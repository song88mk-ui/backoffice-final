using Backoffice.Web.Components.Administration.CancelBets;
using Backoffice.Web.Components.Administration.MaintenanceV2;
using Backoffice.Web.Models;
using Backoffice.Web.Pages.SBC;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Backoffice.Web.Pages.Administration
{
    public partial class MaintenanceV2
    {
        [Inject] public IModalService ModalService { get; set; }

        private string _title = "Maintenance";
        private PageHeaderModel _pageHeaderModel;
        private bool _isMaintenance;
        private string _maintenanceModeAllClientsCss;
        private string _maintenanceModeSpecificClientsCss;

        private bool _isAllClient;

        private Action<EventArgs> _allClientsEvent;
        private Action<EventArgs> _specificClientsEvent;

        private List<string> _clients { get; set; }
        protected override async Task OnInitializedAsync()
        {
            _clients = new List<string>()
            {
                "Alpha88", "EKings Thailand", "Khelraja", "WSGaming" , "W88"
            };

            _pageHeaderModel = new()
            {
                Title = "Maintenance",
                IconClass = "ico ico-maintenance"
            };

            _allClientsEvent = _ => ChangeMode(true);
            _specificClientsEvent = _ => ChangeMode(false);

            ChangeMode(true);
        }

        private void OnChange(ChangeEventArgs args)
        {
            var aw = args.Value.ToString();
            Console.WriteLine($"Selcted = {_isAllClient}");
        }
        private void ChangeMode(bool isAllClient)
        {
            _isAllClient = isAllClient;

            if( _isAllClient )
            {
                _maintenanceModeAllClientsCss = "active";
                _maintenanceModeSpecificClientsCss = "";
            }else
            {
                _maintenanceModeAllClientsCss = "";
                _maintenanceModeSpecificClientsCss = "active";
            }
        }
        private void HandleModalDisplay()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            ModalService.Open<MaintenanceModal>(parameters, options);
        }
    }
}
