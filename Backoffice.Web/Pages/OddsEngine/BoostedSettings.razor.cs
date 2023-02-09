using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
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

        [CascadingParameter(Name = "UserModel")] UserModel UserModel { get; set; }

        protected List<BoostedSettingsModel> ClientsView { get; set; }
        protected SearchFormModel SearchForm { get; set; }
        public List<BreadcrumbModel> Breadcrumbs { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public bool IsPreLoader { get; set; } = true;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Breadcrumbs = new List<BreadcrumbModel>();
            THeadClass = "table-head";
            PageTitle = "Boosted Settings";
            IconClass = string.Empty;

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

            SearchForm = new SearchFormModel();
            ClientsView = new List<BoostedSettingsModel>();

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
                //parameters.Add("ClientCode", BoostedSettings.ClientCode);
                //parameters.Add("ClientName", BoostedSettings.ClientName);
                //parameters.Add("Cap", BoostedSettings.Cap);

                var options = new ModalOptions();
                ModalService.Open<CreateOrUpdateBoostedSettings>("Edit % Cap of Current Odds from Boosted Odds", parameters, options);
            }
            catch (Exception ex)
            {

            }
        }
        public async Task OnViewLogs(BoostedSettingsModel client)
        {
            try
            {
                var parameters = new ModalParameters();

                parameters.Add("Client", client);
                parameters.Add("DialogStyle", "max-width: 80%; overflow-y: initial !important");

                var options = new ModalOptions();
                ModalService.Open<ViewLogsBoostedSettingModalBody>($"View Logs for {client.ClientName}", parameters, options);
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
