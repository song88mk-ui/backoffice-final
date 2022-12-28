using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
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


        [Inject] public ITradingService TradingService { get; set; }
        [CascadingParameter(Name = "UserModel")] UserModel UserModel { get; set; }
        protected List<BoostedSettingsModel> ClientsView { get; set; }
        protected SearchFormModel SearchForm { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
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
        public void Dispose()
        {
        }

        #endregion
    }
}
