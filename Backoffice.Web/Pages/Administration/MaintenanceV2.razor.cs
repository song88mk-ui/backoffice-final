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
        [Inject] public ITradingService TradingService { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        private string _title = "Maintenance";
        private PageHeaderModel _pageHeaderModel;
        private bool _isMaintenance;
        private string _maintenanceModeAllClientsCss;
        private string _maintenanceModeSpecificClientsCss;

        private bool _isAllClient;

        private Action<EventArgs> _allClientsEvent;
        private Action<EventArgs> _specificClientsEvent;

        private List<SelectListItemWithCheckboxModel<string>> _clients { get; set; }
        protected override async Task OnInitializedAsync()
        {
            _pageHeaderModel = new()
            {
                Title = "Maintenance",
                IconClass = "ico ico-maintenance"
            };


            _clients = await LoadClients();

            _allClientsEvent = _ => ChangeMode(_clients.All(c => c.IsChecked));
            _specificClientsEvent = _ => ChangeMode(!_clients.All(c => c.IsChecked));

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
        private async Task<List<SelectListItemWithCheckboxModel<string>>> LoadClients()
        {
            var response = (await TradingService.GetClientList()).Result;
            var result = new List<SelectListItemWithCheckboxModel<string>>();

            if (response != null && response.Any())
            {
                result = response.Select(e =>
                    new SelectListItemWithCheckboxModel<string>()
                    {
                        IsChecked = true,
                        Name = e.ClientName,
                        Value = e.ClientCode.ToString()
                    }).ToList();
            }

            return result;
        }
    }
}
