using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Pages.SBC
{
    public partial class Dashboard : IDisposable
    {
        #region Properties

        [Inject] public ICommonService CommonService { get; set; }
        [Inject] public IConfiguration Configuration { get; set; }
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public string Token { get; set; }        
        public string IFrameSrc { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            await AuthenticateAccess.InvokeAsync();

            PageTitle = "Dashboard";
            IconClass = "ico ico-dashboard";

            Token = CommonService.GenerateToken();

            await Initialize();
        }

        private async Task Initialize()
        {
            var reportRoles = (await AuthenticationService.GetReportRolesUrl(new ReportRolesUrlRequest
            {
                RoleId = UserModel.RoleId,
                ClaimType = SportsbookClientPermission.SBC_Dashboard.ToString().Replace("_", ".")
            })).Result ?? new ReportRolesUrlResponse();

            IFrameSrc = reportRoles.Url;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
