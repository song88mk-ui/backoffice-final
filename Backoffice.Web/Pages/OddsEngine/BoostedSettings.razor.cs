using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.OddsEngine.BoostedSettings;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Serilog;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class BoostedSettings : IDisposable
    {
        #region Properties

        [Inject] public ITradingService TradingService { get; set; }
        [Inject] IModalService ModalService { get; set; }
        [Parameter] public string Region { get; set; } = "hk";
        [CascadingParameter(Name = "UserClientCodes")] List<string>? UserClientCodes { get; set; }
        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }

        //protected SearchFormModel SearchForm { get; set; } = new SearchFormModel();
        #endregion
        private AlertModel _alertModel;

        private bool _isPreLoader;
        private PageHeaderModel _pageHeaderModel;
        private List<BreadcrumbModel> _breadcrumbs;
        private string _tHeadClass;
        private string _searchText;
        protected List<BoostedSettingsModel> _clientsView;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _isPreLoader = true;
            _tHeadClass = "table-head";
            _pageHeaderModel = new()
            {
                Title = "Boosted Settings",
                IconClass = string.Empty
            };

            TradingService.Region = Region.ToUpper();
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

            await AuthenticateAccess.InvokeAsync();
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

                var result = await TradingService.GetBoostedSettings(new BoostedSettingsRequest()
                {
                    ClientCodes = UserClientCodes != null && UserClientCodes.Any() ? UserClientCodes.ToList() : new List<string>(),
                    Search = _searchText
                });
                _clientsView = result.Result;
                _isPreLoader = false;
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
                parameters.Add("Region", Region);

                var options = new ModalOptions();
                ModalService.Open<CreateOrUpdateBoostedSettings>("Edit % Cap of Current Odds from Boosted Odds", parameters, options);
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

                ModalService.Open<ViewAllLogsModal>($"View Logs", parameters, options);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }
        }

        private void OpacityChanged(decimal opacity)
        {
            _alertModel = new();
        }
        public void Dispose()
        {
            TradingService.Dispose();
        }

        #endregion
    }
}
