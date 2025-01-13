using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.OddsEngine.BoostedSettings;
using Backoffice.Web.Models;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components;
using Serilog;
using System.ComponentModel;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class BoostedSettings : IDisposable
    {
        #region Properties
        [Parameter] public string Region { get; set; } = "HK";

        #endregion
        private AlertModel _alertModel;

        private bool _isPreLoader;
        private PageHeaderModel _pageHeaderModel;
        private List<BreadcrumbModel> _breadcrumbs;
        private string _tHeadClass;
        private string _searchText;
        protected List<BoostedSettingsModel> _clientsView;
        private List<ClientInformationModel>? _clientListReference = null;
        private CancellationTokenSource _cts;

        private bool _init = true;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _isPreLoader = true;
            _tHeadClass = "table-head";
            _pageHeaderModel = new()
            {
                Title = "Boosted Settings",
                IconClass = string.Empty,
                ShowLanguageDropDown = false
            };

            _tradingService.Region = Region.ToUpper();
            _alertModel = new();

            _breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Price Boost",
                    Url = string.Empty,
                    IsCurrentPage = false
                },
                new BreadcrumbModel
                {
                    Name = "Boosted Settings",
                    Url = string.Empty,
                    IsCurrentPage = true
                }
            };

            _userState.PropertyChanged += UserStatePropertyChanged;
            await PageState.AuthenticateAccessEvent.InvokeAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (_userState.TradingClients != null && _userState.TradingClients.Any() && !_clientListReference.Any())
            {
                _clientListReference = new List<ClientInformationModel>(_userState.TradingClients);
                _init = true;
            }

            var shouldRender = ValidateParameter();

            if (shouldRender)
                await GetContent();

            _init = false;
        }

        public async Task OnSaveSettingClickEvent(object item)
        {
            _alertModel = new()
            {
                Message = "% Cap has been successfully updated!",
                Status = AlertStatus.Success,
                Opacity = 1
            };

            await GetContent();
        }

        private async Task GetContent(string search = "")
        {
            try
            {
                if (!_isPreLoader)
                {
                    _isPreLoader = true;
                }
                _searchText = search ?? string.Empty;

                var clientCodes = _clientListReference.Select(s => s.ClientCode).ToList();

                _cts?.Cancel();
                _cts = new CancellationTokenSource();

                var token = _cts.Token;
                var result = await _tradingService.GetBoostedSettings(new BoostedSettingsRequest()
                {
                    ClientCodes = clientCodes.Any() ? clientCodes : new List<string>(),
                    Search = _searchText
                }, token);


                if (!token.IsCancellationRequested)
                {
                    _clientsView = result.Result;
                    _isPreLoader = false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }
        }
        public async Task OnEditSetting(BoostedSettingsModel client)
        {
            try
            {
                var parameters = new ModalParameters();

                parameters.Add("Client", client);
                parameters.Add("Region", "hk");

                var options = new ModalOptions();
                _modalService.Open<CreateOrUpdateBoostedSettings>("Edit % Cap of Current Odds from Boosted Odds", parameters, options);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }
        }
        public async Task OnViewAllLogsClickEvent()
        {
            try
            {
                var parameters = new ModalParameters();
                parameters.Add("DialogStyle", "max-width: 80%; overflow-y: initial !important");
                parameters.Add("SelectedPage", SportsbookClientPermission.SBC_OE_PriceBoost_Settings);
                parameters.Add("Region", Region);
                var options = new ModalOptions();

                _modalService.Open<ViewAllLogsModal>($"View Logs", parameters, options);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }
        }

        private bool ValidateParameter() => _clientListReference != null && _init;
        private async void UserStatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_userState.TradingClients != null && _userState.TradingClients.Any() && !_clientListReference.Any())
            {
                _clientListReference = new List<ClientInformationModel>(_userState.TradingClients);

                await GetContent();
            }
        }
        private void OpacityChanged(decimal opacity)
        {
            _alertModel = new();
        }
        public void Dispose()
        {
        }

        #endregion
    }
}
