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
        [Inject] public ITableauService TableauService { get; set; }
        [Inject] public IAuthenticationService AuthenticationService { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public string IFrameSrc { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            await AuthenticateAccess.InvokeAsync();

            PageTitle = "Sports";
            IconClass = "ico ico-sports";


            await Initialize();
        }

        private async Task Initialize()
        {
            var reportRoles = (await AuthenticationService.GetReportRolesUrl(new ReportRolesUrlRequest
            {
                RoleId = UserModel.RoleId,
                ClaimType = AdminPermission.Admin_Dashboard.ToString().Replace("_", ".")
            })).Result ?? new ReportRolesUrlResponse();

            IFrameSrc = await TableauService.GetTableauUrl(reportRoles.Url);
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
