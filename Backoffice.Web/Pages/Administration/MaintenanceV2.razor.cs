using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Administration.MaintenanceV2;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

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
        private bool _isAllClientMode = true;
        private bool _isAllClient = false;
        private bool _isMaintenance;
        private bool _isPreloader;
        private bool _hasChange;
        private bool _isProcessing;
        private bool _isDropDownDisabled;
        private bool _clienHasChange;
        public bool IsMaintenance
        {
            get { return _isMaintenance; }
            set
            {
                if (value == _isMaintenance)
                    return;

                HandleModalDisplay(true, value);
                _shouldRender = true;
            }
        }

        private Action<EventArgs> _allClientsEvent;
        private Action<EventArgs> _specificClientsEvent;

        private bool _initialStatusIsAllClient;
        private bool _initialMaintenanceIsOn;
        private List<string> _liveClients = new();
        private List<string> _underMaintenanceClients = new();

        private List<MaintenanceSelectListItemWithCheckboxModel> _clients { get; set; }
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


                _isAllClient = (_underMaintenanceClients.Any() && !_liveClients.Any()) || (!_underMaintenanceClients.Any() && _liveClients.Any());
                _isDropDownDisabled = _underMaintenanceClients.Any();

                HandleChangeMode(_isAllClientMode);
                _initialStatusIsAllClient = _isAllClientMode;
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
            if (_hasChange)
                HandleModalDisplay(false, isAllClient);
            else
                HandleChangeMode(isAllClient);
        }
        private void HandlePropertyChange(bool value)
        {
            var hasChange = _hasChange;
            if (!_isAllClientMode)
            {
                if (value != _clienHasChange)
                    _clienHasChange = value;

                hasChange = (_clienHasChange || _isDropDownDisabled) && _isMaintenance != _initialMaintenanceIsOn && _clients.Any(c => c.IsChecked);
            }
            else
            {
                hasChange = _isMaintenance != _initialMaintenanceIsOn;
            }

            if (hasChange != _hasChange)
            {
                _hasChange = hasChange;
                _shouldRender = true;
            }
        }
        private void HandleModalSaveClick(object content)
        {
            var response = (Tuple<bool, bool>)content;

            if (response.Item1)
            {
                _isMaintenance = response.Item2;
                _shouldRender = true;

                HandlePropertyChange(_clienHasChange);
            }
            else
            {
                HandleChangeMode(response.Item2);
            }
        }
        private void HandleChangeMode(bool isAllClient)
        {
            _hasChange = false;

            _isAllClientMode = isAllClient;

            if (_isAllClientMode)
            {
                _maintenanceModeAllClientsCss = "active";
                _maintenanceModeSpecificClientsCss = "";

                _isMaintenance = _maintenanceConfigModels.All(c => c.IsMaintenance);
                _initialMaintenanceIsOn = _isMaintenance;
            }
            else
            {
                _maintenanceModeAllClientsCss = "";
                _maintenanceModeSpecificClientsCss = "active";

                _isMaintenance = _clients.Any(c => c.IsMaintenanceEnabled);
                _initialMaintenanceIsOn = _isMaintenance;
            }

            _shouldRender = true;
        }
        private async void HandleSaveClick()
        {
            _isProcessing = true;
            _shouldRender = true;

            var clientIds = string.Empty;
            if (_isAllClientMode)
                clientIds = "All";
            else
                clientIds = string.Join(",", _clients.Where(c => c.IsChecked).Select(c => c.Value).ToList());

            await _alertToastService.SetLoadingToast($"{(_isMaintenance ? "Enabling" : "Disabling")} Maintenance");

            var result = await _backofficeService.SetMaintenanceV2(new SetMaintenanceRequest()
            {
                ClientIds = clientIds,
                IsMaintenance = _isMaintenance
            });

            _isProcessing = false;

            if (result.StatusCode == StatusCode.Success)
                await _alertToastService.SetToast($"Maintenance Mode {(_isMaintenance ? "Enabled" : "Disabled")}");
            else
                await _alertToastService.SetToast($"Maintenance Mode {(_isMaintenance ? "Enabled" : "Disabled")}");

            LoadPage();

        }
        private void HandleCancel() => LoadPage();

        private void HandleModalDisplay(bool isMaintenanceSwitch, bool value)
        {
            var parameters = new ModalParameters();

            parameters.Add("Value", value);
            parameters.Add("IsMainteanceSwitch", isMaintenanceSwitch);
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            _modalService.Open<MaintenanceModal>(parameters, options);
        }

        private List<MaintenanceConfigModel> _maintenanceConfigModels = new List<MaintenanceConfigModel>();
        private async Task<List<MaintenanceSelectListItemWithCheckboxModel>> LoadClients()
        {
            var response = await _backofficeService.GetMaintenanceV2();
            var result = new List<MaintenanceSelectListItemWithCheckboxModel>();

            if (response.StatusCode != StatusCode.Success)
                return result;

            _maintenanceConfigModels = response?.Result?.ClientConfig;

            if (_maintenanceConfigModels != null && _maintenanceConfigModels.Any())
            {
                result = _maintenanceConfigModels.Select(e =>
                    new MaintenanceSelectListItemWithCheckboxModel()
                    {
                        IsChecked = e.IsMaintenance,
                        Name = _maintenanceConfigModels.Count(c => c.ClientId == e.ClientId) > 1 ? $"{e.App}-{e.ClientName.ToString()}" : $"{e.ClientName.ToString()}",
                        Value = _maintenanceConfigModels.Count(c => c.ClientId == e.ClientId) > 1 ? $"{e.App}:{e.ClientId.ToString()}" : $"{e.ClientId.ToString()}",
                        IsMaintenanceEnabled = e.IsMaintenance
                    }).ToList();
            }
            else
                _maintenanceConfigModels = new List<MaintenanceConfigModel>();

            return result;
        }
    }
}
