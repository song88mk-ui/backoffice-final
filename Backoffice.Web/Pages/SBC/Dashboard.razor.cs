using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;

namespace Backoffice.Web.Pages.SBC
{
    public partial class Dashboard : IDisposable
    {
        #region Fields

        private string _iFrameSrc;
        private string _roleId;
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

        }

        protected override async Task OnParametersSetAsync()
        {
            if (_roleId != _userState.Model.RoleId)
            {
                _roleId = _userState.Model.RoleId;
                await Initialize();
            }
        }

        private async Task Initialize()
        {
            try
            {
                var reportRoles = (await _authenticationService.GetReportRolesUrl(new ReportRolesUrlRequest
                {
                    RoleId = _roleId,
                    ClaimType = SportsbookClientPermission.SBC_Dashboard.ToString().Replace("_", ".")
                })).Result ?? new ReportRolesUrlResponse();

                _iFrameSrc = await _tableauService.GetTableauUrl(reportRoles.Url);
            }
            catch (Exception) { }
            finally
            {
                _isPreLoader = false;
            }
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
