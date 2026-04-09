using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.Administration
{
    public partial class Dashboard : IDisposable
    {
        #region Properties
        [Inject] public MetabaseService MetabaseService { get; set; }

        public string IFrameSrc { get; set; }

        #endregion

        private bool _isPreLoader = true;

        private PageHeaderModel _pageHeaderModel;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _pageHeaderModel = new()
            {
                Title = "Dashboard",
                IconClass = "ico ico-sports"
            };

            await PageState.AuthenticateAccessEvent.InvokeAsync();

            LoadEmbedUrl();
        }

        private void LoadEmbedUrl()
        {
            _isPreLoader = true;
            IFrameSrc = MetabaseService.GetEmbedUrl(_userState.Model.Name);
            _isPreLoader = false;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
