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

        [Parameter] public string THeadClass { get; set; }
        [Parameter] public string Region { get; set; } = "hk";

        [CascadingParameter(Name = "UserClientCodes")] List<string>? UserClientCodes { get; set; }
        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }

        protected List<BoostedSettingsModel> ClientsView { get; set; }
        protected SearchFormModel SearchForm { get; set; } = new SearchFormModel();
        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public bool IsPreLoader { get; set; } = true;

        #endregion

        private PageHeaderModel _pageHeaderModel;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            THeadClass = "table-head";
            _pageHeaderModel = new()
            {
                Title = "Boosted Settings",
                IconClass = string.Empty
            };

            TradingService.Region = Region.ToUpper();

            Breadcrumbs = new List<BreadcrumbModel>()
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
            ClientsView = new List<BoostedSettingsModel>();

            await AuthenticateAccess.InvokeAsync();

            await GetContent();
        }

        public async Task OnSaveSettingClickEvent(object item)
        {
            await GetContent();
        }

        private async Task GetContent()
        {
            try
            {
                if (!IsPreLoader)
                {
                    IsPreLoader = true;
                }


                var result = await TradingService.GetBoostedSettings(new BoostedSettingsRequest()
                {
                    ClientCodes = UserClientCodes != null && UserClientCodes.Any() ? UserClientCodes.ToList() : new List<string>(),
                    Search = SearchForm.SearchText ?? string.Empty
                });
                ClientsView = result.Result;
                IsPreLoader = false;
                StateHasChanged();
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

        public void Dispose()
        {
            TradingService.Dispose();
        }

        #endregion
    }
}
