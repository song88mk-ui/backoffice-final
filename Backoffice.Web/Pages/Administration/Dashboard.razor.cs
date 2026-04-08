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
        private IReadOnlyList<string> _sports = Array.Empty<string>();
        private string _selectedSport;

        private PageHeaderModel _pageHeaderModel;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _pageHeaderModel = new()
            {
                Title = "Sports",
                IconClass = "ico ico-sports"
            };

            _sports = MetabaseService.Sports;
            _selectedSport = _sports.FirstOrDefault() ?? string.Empty;

            await PageState.AuthenticateAccessEvent.InvokeAsync();

            LoadEmbedUrl();
        }

        private void LoadEmbedUrl()
        {
            _isPreLoader = true;

            if (!string.IsNullOrEmpty(_selectedSport))
                IFrameSrc = MetabaseService.GetEmbedUrl(_selectedSport);

            _isPreLoader = false;
        }

        private void OnSportChanged(ChangeEventArgs e)
        {
            _selectedSport = e.Value?.ToString() ?? string.Empty;
            LoadEmbedUrl();
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
