using Backoffice.Shared.Models.Response;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Security;

namespace Backoffice.Web.Pages.Administration
{
    public partial class ManageUser : IDisposable
    {
        #region Properties
        [Inject] public IAuthenticationService AuthenticationService { get; set; }

        public ManageUserListResponse UserList {  get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public bool IsPreLoader { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            IsPreLoader = true;
            PageTitle = "Manage User";
            IconClass = "ico ico-manage-user";

            UserList = new ManageUserListResponse();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if(firstRender)
            {
                await GetContent();
            }
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        private async Task GetContent()
        {
            var userResponse = await AuthenticationService.GetUserList();
            if (userResponse.Result != null)
                UserList = userResponse.Result;

            IsPreLoader = false;
            StateHasChanged();
        }
        #endregion
    }
}
