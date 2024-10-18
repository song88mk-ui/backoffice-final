using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class Dashboard : IDisposable
    {
        #region Properties

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        #endregion

        #region Fields

        public string _iFrameSrc;

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

            await Initialize();
        }

        private async Task Initialize()
        {
            var reportRoles = (await _authenticationService.GetReportRolesUrl(new ReportRolesUrlRequest
            {
                RoleId = UserModel.RoleId,
                ClaimType = SportsbookClientPermission.SBC_Dashboard.ToString().Replace("_", ".")
            })).Result ?? new ReportRolesUrlResponse();

            _iFrameSrc = await _tableauService.GetTableauUrl(reportRoles.Url);
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
