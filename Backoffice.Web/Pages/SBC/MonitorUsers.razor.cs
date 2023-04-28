using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class MonitorUsers : IDisposable
    {
        #region Properties

        [Inject] public ICommonService CommonService { get; set; }
        [Inject] public IConfiguration Configuration { get; set; }
        [Inject] public IAuthenticationService AuthenticationService { get; set; }

        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public string Token { get; set; }
        public string IFrameSrc { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            await AuthenticateAccess.InvokeAsync();

            PageTitle = "Monitor Users";
            IconClass = "ico ico-monitor-users";


            await Initialize();
        }

        private async Task Initialize()
        {
            var reportRoles = (await AuthenticationService.GetReportRolesUrl(new ReportRolesUrlRequest
            {
                RoleId = UserModel.RoleId,
                ClaimType = SportsbookClientPermission.SBC_Reports_MonitorUsers.ToString().Replace("_", ".")
            })).Result ?? new ReportRolesUrlResponse();

            IFrameSrc = reportRoles.Url;
            Token = CommonService.GenerateToken();
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
