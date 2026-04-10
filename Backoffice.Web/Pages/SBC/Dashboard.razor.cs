using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class Dashboard : IDisposable
    {
        #region Properties

        [Inject] public MetabaseService MetabaseService { get; set; }

        #endregion

        #region Fields

        private string _iFrameSrc;
        private bool _isPreLoader = true;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            PageHeaderModel = new PageHeaderModel()
            {
                Title = "Dashboard",
                IconClass = "ico ico-dashboard"
            };

            await PageState.AuthenticateAccessEvent.InvokeAsync();

            LoadEmbedUrl();
        }

        private void LoadEmbedUrl()
        {
            _isPreLoader = true;
            _iFrameSrc = MetabaseService.GetEmbedUrl(_userState.Model.Name);
            _isPreLoader = false;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
