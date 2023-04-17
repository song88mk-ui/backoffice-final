using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.OddsEngine.BoostedSettings;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class BoostedSettings : IDisposable
    {
        #region Properties

        [Inject] public ITradingService TradingService { get; set; }
        [Inject] IModalService ModalService { get; set; }

        [Parameter] public string THeadClass { get; set; }
        [Parameter] public string Region { get; set; } = "hk";

        [CascadingParameter(Name = "UserModel")] UserModel UserModel { get; set; }

        protected List<BoostedSettingsModel> ClientsView { get; set; }
        protected SearchFormModel SearchForm { get; set; } = new SearchFormModel();
        public List<BreadcrumbModel> Breadcrumbs { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public bool IsPreLoader { get; set; } = true;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            THeadClass = "table-head";
            PageTitle = "Boosted Settings";
            IconClass = string.Empty;
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
        }

        public async Task OnSaveSettingClickEvent(object item)
        {
            await GetContent();
        }

        private async Task GetContent()
        {
            try
            {
                if(!IsPreLoader)
                {
                    IsPreLoader = true;
                }

                var result = await TradingService.GetBoostedSettings(new BoostedSettingsRequest()
                {
                    ClientCodes = UserModel.ClientCodes.ToList(),
                    Search = SearchForm.SearchText
                });
                ClientsView = result.Result;
                IsPreLoader = false;
                StateHasChanged();
            }
            catch (Exception)
            {

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

            }
        }

        public void Dispose()
        {
            TradingService.Dispose();
        }

        #endregion
    }
}
