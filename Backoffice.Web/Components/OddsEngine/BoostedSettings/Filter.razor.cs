using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.OddsEngine.ManageBoost;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Backoffice.Web.Components.OddsEngine.BoostedSettings
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        [Parameter] public SearchFormModel SearchForm { get; set; }
        [Parameter] public EventCallback<SearchFormModel> SearchFormChanged { get; set; }
        [Parameter] public EventCallback OnInitializeCallback { get; set; }
        [Parameter] public EventCallback OnViewAllLogsCallback { get; set; }

        public SportsbookClientPermission CurrentPage { get; set; }

        public bool ShowManageBoostMenu { get; set; }
        public bool ShowBoostedSettingsMenu { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            var page = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "");

            if (page.Contains("manage") || page == string.Empty)
            {
                ShowManageBoostMenu = true;
                CurrentPage = SportsbookClientPermission.SBC_OE_PriceBoost_Manage;
            }
            else if (page.Contains("settings"))
            {
                ShowBoostedSettingsMenu = true;
                CurrentPage = SportsbookClientPermission.SBC_OE_PriceBoost_Settings;
            }

            SearchForm = new SearchFormModel();
            await SearchFormChanged.InvokeAsync(SearchForm);
            await OnInitializeCallback.InvokeAsync();
        }
        public async Task OnInputSearchEvent()
        {
            await SearchFormChanged.InvokeAsync(SearchForm);
            await OnInitializeCallback.InvokeAsync();
        }

        public async void OnViewLogsClickEvent()
        {
            await OnViewAllLogsCallback.InvokeAsync();
        }
        public void Dispose()
        {
        }

        #endregion
    }
}
