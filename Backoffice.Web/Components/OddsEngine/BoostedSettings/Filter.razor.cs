using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.ManageBoost;
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
        public SearchFormModel SearchForm { get; set; }
        public SportsbookClientPermission CurrentPage { get; set; }
        public bool ShowManageBoostMenu { get; set; }
        public bool ShowBoostedSettingsMenu { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SearchForm = new SearchFormModel();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
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
        }

        public async Task OnInputSearchEvent()
        {
        }
        public void OnViewLogsClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("DialogStyle", "max-width: 80%; overflow-y: initial !important");
            parameters.Add("SelectedPage", CurrentPage);
            var options = new ModalOptions();

            ModalService.Open<ViewAllLogsModal>($"View Logs", parameters, options);
        }
        public void Dispose()
        {
        }

        #endregion
    }
}
