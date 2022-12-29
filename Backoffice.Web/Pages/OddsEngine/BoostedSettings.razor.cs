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

        public string PageTitle { get; set; }
        public string IconClass { get; set; }


        [Parameter] public string THeadClass { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [CascadingParameter(Name = "UserModel")] UserModel UserModel { get; set; }
        protected List<BoostedSettingsModel> ClientsView { get; set; }
        protected SearchFormModel SearchForm { get; set; }
        [Inject] IModalService ModalService { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            THeadClass = "table-head";
            PageTitle = "Boosted Settings";
            IconClass = string.Empty;

            SearchForm = new SearchFormModel();
            ClientsView = new List<BoostedSettingsModel>();
            GetContent();
        }

        private async Task GetContent()
        {
            await InvokeAsync(StateHasChanged);

            try
            {
                ClientsView = (await TradingService.GetBoostedSettings(new BoostedSettingsRequest()
                {
                    ClientCodes = UserModel.ClientCodes.ToList(),
                    Search = SearchForm.SearchText
                })).Result;
            }
            catch (Exception)
            {

            }
            finally
            {
                await InvokeAsync(StateHasChanged);
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
        public void Dispose()
        {
        }

        #endregion
    }
}
