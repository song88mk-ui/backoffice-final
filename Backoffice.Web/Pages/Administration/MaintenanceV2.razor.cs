using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Administration.MaintenanceV2;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

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
        private bool _isAllClient = true;
        private bool _isPreloader;
        private bool _hasChange;
        private bool _isProcessing;

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
            await PageState.AuthenticateAccessEvent.InvokeAsync();

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
            try
            {
                _isPreloader = true;
                _shouldRender = true;
                _hasChange = false;
                _clients = await LoadClients();

                _liveClients = _maintenanceConfigModels.Where(c => c.IsMaintenance == false).Select(c => c.ClientName).ToList();
                _underMaintenanceClients = _maintenanceConfigModels.Where(c => c.IsMaintenance == true).Select(c => c.ClientName).ToList();

                _liveClients = _clients.Where(c => !c.IsChecked).Select(s => s.Value).ToList();
                _underMaintenanceClients = _clients.Where(c => c.IsChecked).Select(s => s.Value).ToList();

                ChangeMode(_isAllClient);
                _initialStatusIsAllClient = _isAllClient;
                _initialMaintenanceIsOn = _isMaintenance;
            }
            catch (Exception ex)
            {
            }
            finally
            {
                _isPreloader = false;
                _shouldRender = true;
                StateHasChanged();
            }
        }
        private void ChangeMode(bool isAllClient)
        {
            _isAllClient = isAllClient;

            if (_isAllClient)
            {
                _maintenanceModeAllClientsCss = "active";
                _maintenanceModeSpecificClientsCss = "";

                _isMaintenance = _maintenanceConfigModels.All(c => c.IsMaintenance);
            }
            else
            {
                _maintenanceModeAllClientsCss = "";
                _maintenanceModeSpecificClientsCss = "active";

                _isMaintenance = _clients.Any(c => c.IsChecked);
            }

            _shouldRender = true;
        }

        private void HandlePropertyChange(bool value = false)
        {
            var hasChange = value || _isMaintenance != _initialMaintenanceIsOn || _isAllClient != _initialStatusIsAllClient;

            if (hasChange != _hasChange)
            {
                _hasChange = hasChange;
                _shouldRender = true;
            }
        }

        public async void HandleSaveClick()
        {
            _isProcessing = true;
            _shouldRender = true;

            var clientIds = string.Empty;
            if (_isAllClient)
                clientIds = "All";
            else
                clientIds = string.Join(",", _clients.Where(c => c.IsChecked).Select(c => c.Value).ToList());

            var result = await _backofficeService.SetMaintenanceV2(new SetMaintenanceRequest()
            {
                ClientIds = clientIds,
                IsMaintenance = _isMaintenance
            });

            _isProcessing = false;
            LoadPage();
        }
        public void HandleCancelClick()
        {
            _isMaintenance = _initialMaintenanceIsOn;
            LoadPage();
        }
        private void HandleMaintenanceSwitch() => HandlePropertyChange();
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
        private List<MaintenanceConfigModel> _maintenanceConfigModels = new List<MaintenanceConfigModel>();
        private async Task<List<SelectListItemWithCheckboxModel<string>>> LoadClients()
        {
            var response = await _backofficeService.GetMaintenanceV2();
            var result = new List<SelectListItemWithCheckboxModel<string>>();

            if (response.StatusCode != StatusCode.Success)
                return result;

            _maintenanceConfigModels = response?.Result?.ClientConfig;

            if (_maintenanceConfigModels != null && _maintenanceConfigModels.Any())
            {
                result = _maintenanceConfigModels.Select(e =>
                    new SelectListItemWithCheckboxModel<string>()
                    {
                        IsChecked = e.IsMaintenance,
                        Name = _maintenanceConfigModels.Count(c => c.ClientId == e.ClientId) > 1 ? $"{e.App}-{e.ClientName.ToString()}" : $"{e.ClientName.ToString()}",
                        Value = _maintenanceConfigModels.Count(c => c.ClientId == e.ClientId) > 1 ? $"{e.App}:{e.ClientId.ToString()}" : $"{e.ClientId.ToString()}"
                    }).ToList();
            }
            else
                _maintenanceConfigModels = new List<MaintenanceConfigModel>();

            return result;
        }
        private void HandleOnSaveClick()
        {

        }
        private void HandelOnCancel() => LoadPage();
    }
}
