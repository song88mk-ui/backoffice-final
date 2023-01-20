using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.ManageUser;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.Administration
{
    public partial class ManageUser : IDisposable
    {
        #region Properties
        [Inject] public IAuthenticationService AuthenticationService { get; set; }

        [Inject] public IModalService ModalService { get; set; }

        public ManageUserFilterModel FilterModel { get; set; }
        public List<UserResponse> UserList {  get; set; }
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

            FilterModel = new ManageUserFilterModel();
            UserList = new List<UserResponse>();
        }
        public async Task Init()
        {
            IsPreLoader = true;
            await GetContent();
        }
        public async Task OnCreateUserClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("DialogStyle", "max-width: 65rem;");

            var options = new ModalOptions();

            ModalService.Open<CreateOrUpdateModal>("Create User", parameters, options);
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        private async Task GetContent()
        {
            var userResponse = await AuthenticationService.GetUserList(new UserGroupRequest()
            { 
                SearchText = FilterModel.SearchText,
                SortBy = FilterModel.UserSorting
            });
            if (userResponse.Result != null)
                UserList = userResponse.Result.Users;

            IsPreLoader = false;
            StateHasChanged();
        }
        #endregion
    }
}
