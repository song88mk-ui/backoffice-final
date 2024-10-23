using Azure;
using Backoffice.Shared.Enums;
using Backoffice.Web.Components.Administration.CancelBets;
using Backoffice.Web.Components.Administration.MaintenanceV2;
using Backoffice.Web.Models;
using Backoffice.Web.Pages.SBC;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RabbitMQ.Client.Framing.Impl;

namespace Backoffice.Web.Pages.Administration
{
    public partial class MaintenanceV2
    {
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        private string _title = "Maintenance";
        private PageHeaderModel _pageHeaderModel;
        private string _maintenanceModeAllClientsCss;
        private string _maintenanceModeSpecificClientsCss;

        private bool _init = true;
        private bool _shouldRender = true;
        private bool _isMaintenance;
        private bool _isAllClient;
        private bool _isPreloader;

        private Action<EventArgs> _allClientsEvent;
        private Action<EventArgs> _specificClientsEvent;
        //private bool _hasChange;

        private bool _initialStatusIsAllClient;
        private bool _initialMaintenanceIsOn;
        private List<string> _liveClients = new();
        private List<string> _underMaintenanceClients = new();

        private List<SelectListItemWithCheckboxModel<string>> _clients { get; set; }
        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _pageHeaderModel = new()
            {
                Title = "Maintenance",
                IconClass = "ico ico-maintenance"
            };

            _allClientsEvent = _ => ChangeMode(true);
            _specificClientsEvent = _ => ChangeMode(false);

            LoadPage();
        }
        protected override void OnParametersSet()
        {
            _init = false;
        }
        protected override bool ShouldRender() => _shouldRender;
        protected override void OnAfterRender(bool firstRender) => _shouldRender = false;

        private async void LoadPage()
        {
            _isPreloader = true;
            _shouldRender = true;
            _clients = await LoadClients();

            var isAllClient = true;

            // Get Maintenance

            _liveClients = _clients.Where(c => !c.IsChecked).Select(s => s.Value).ToList();
            _underMaintenanceClients = _clients.Where(c => c.IsChecked).Select(s => s.Value).ToList();

            ChangeMode(isAllClient);
            _initialStatusIsAllClient = isAllClient;
            _initialMaintenanceIsOn = _isMaintenance;
            _isPreloader = false;
            _shouldRender = true;
        }
        private void ChangeMode(bool isAllClient)
        {
            _isAllClient = isAllClient;

            if (_isAllClient)
            {
                _maintenanceModeAllClientsCss = "active";
                _maintenanceModeSpecificClientsCss = "";
            }
            else
            {
                _maintenanceModeAllClientsCss = "";
                _maintenanceModeSpecificClientsCss = "active";
            }

            _shouldRender = true;
        }

        private void HandlePropertyChange(bool value = false)
        {
            var hasChange = value || _isMaintenance != _initialMaintenanceIsOn || _isAllClient != _initialStatusIsAllClient;

            if (hasChange)
                _saveOptionService.Open();
            else
                _saveOptionService.Close();
        }

        private void HandleMaintenanceSwitch()
        {
            HandlePropertyChange();
        }
        private async void HandleSetAlertToast()
        {
            _alertToastService.SetToast("Success");
        }
        private void HandleModalDisplay()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            _modalService.Open<MaintenanceModal>(parameters, options);
        }
        private async Task<List<SelectListItemWithCheckboxModel<string>>> LoadClients()
        {
            var response = await _backofficeService.GetMaintenanceV2();
            var result = new List<SelectListItemWithCheckboxModel<string>>();

            if (response.StatusCode != StatusCode.Success)
                return result;

            var clientConfigs = response?.Result?.ClientConfig;
            if (clientConfigs != null && clientConfigs.Any())
            {
                result = clientConfigs.Select(e =>
                    new SelectListItemWithCheckboxModel<string>()
                    {
                        IsChecked = true,
                        Name = e.ClientName,
                        Value = e.ClientId.ToString()
                    }).ToList();
            }

            return result;
        }
        private async Task<List<SelectListItemWithCheckboxModel<string>>> LoadClientsTest()
        {
            var test = new[] { "TSO", "EKT", "MHB", "TSO2", "EKT2", "MHB2", "TSO3", "EKT3", "MHB3" };
            var testAw = new List<string>()
            {
                "Alpha88", "EKings Thailand", "Khelraja", "WSGaming" , "W88"
            };

            var rand = new Random();
            return test.Select(e =>
                new SelectListItemWithCheckboxModel<string>()
                {
                    IsChecked = rand.Next(0, 10) % 2 == 0,
                    Name = e,
                    Value = e
                }).ToList();
        }
        private void HandleOnSaveClick()
        {

        }
        private void HandelOnCancel() => LoadPage();
    }
}
