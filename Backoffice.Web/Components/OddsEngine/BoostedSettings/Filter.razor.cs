using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.BoostedSettings
{
    public partial class Filter : IDisposable
    {
        #region Properties


        //[Parameter] public EventCallback<SearchFormModel> SearchFormChanged { get; set; }
        [Parameter] public EventCallback<string> OnInitializeCallback { get; set; }
        [Parameter] public EventCallback OnViewAllLogsCallback { get; set; }

        public SportsbookClientPermission CurrentPage { get; set; }
        private SearchFormModel _searchForm { get; set; }

        public bool ShowManageBoostMenu { get; set; }
        public bool ShowBoostedSettingsMenu { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            var page = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "");

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

            _searchForm = new SearchFormModel();
        }
        public async Task OnInputSearchEvent()
        {
            //await SearchFormChanged.InvokeAsync(SearchForm);
            await OnInitializeCallback.InvokeAsync(_searchForm.SearchText);
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
