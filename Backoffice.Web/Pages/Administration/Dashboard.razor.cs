using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.Administration
{
    public partial class Dashboard : IDisposable
    {
        #region Properties
        [Inject] public MetabaseService MetabaseService { get; set; }
        [Inject] public IAuthenticationService AuthenticationService { get; set; }

        public string IFrameSrc { get; set; }

        #endregion

        private bool _isTableauPreLoader = true;
        private bool _isMetabasePreLoader = true;
        private string? _tableauSrc;
        private string? _roleId;
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
        }

        protected override async Task OnParametersSetAsync()
        {
            if (_roleId != _userState.Model.RoleId)
            {
                _roleId = _userState.Model.RoleId;
                await LoadTableauUrl();
                LoadEmbedUrl();
            }
        }

        private async Task LoadTableauUrl()
        {
            try
            {
                var reportRoles = (await _authenticationService.GetReportRolesUrl(new ReportRolesUrlRequest
                {
                    RoleId = _roleId,
                    ClaimType = AdminPermission.Admin_Dashboard.ToString().Replace("_", ".")
                })).Result ?? new ReportRolesUrlResponse();

                _tableauSrc = await _tableauService.GetTableauUrl(reportRoles.Url);
            }
            catch (Exception) { }
            finally
            {
                _isTableauPreLoader = false;
            }
        }

        private void LoadEmbedUrl()
        {
            _isMetabasePreLoader = true;

            if (!string.IsNullOrEmpty(_selectedSport))
                IFrameSrc = MetabaseService.GetEmbedUrl(_selectedSport);

            _isMetabasePreLoader = false;
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
