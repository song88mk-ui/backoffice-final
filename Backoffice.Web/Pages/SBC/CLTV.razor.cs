using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class CLTV : IDisposable
    {
        #region Properties

        [Inject] public ITableauService TableauService { get; set; }
        [Inject] public IAuthenticationService AuthenticationService { get; set; }

        
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public string IFrameSrc { get; set; }

        #endregion

        private PageHeaderModel _pageHeaderModel;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _pageHeaderModel = new()
            {
                Title = "CLTV",
                IconClass = "ico ico-cltv"
            };
            await PageState.AuthenticateAccessEvent.InvokeAsync();



            await Initialize();
        }

        private async Task Initialize()
        {
            var reportRoles = (await AuthenticationService.GetReportRolesUrl(new ReportRolesUrlRequest
            {
                RoleId = UserModel.RoleId,
                ClaimType = SportsbookClientPermission.SBC_Reports_CLTV.ToString().Replace("_", ".")
            })).Result ?? new ReportRolesUrlResponse();

            IFrameSrc = await TableauService.GetTableauUrl(reportRoles.Url);
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
