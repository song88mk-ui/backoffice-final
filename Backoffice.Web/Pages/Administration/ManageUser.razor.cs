using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.ManageUser;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Backoffice.Web.Pages.Administration
{
    public partial class ManageUser : IDisposable
    {
        #region Properties
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        private MetaData MetaData { get; set; }
        public PermissionResponse Permissions { get; set; }
        public ManageUserFilterModel FilterModel { get; set; } = new ManageUserFilterModel();
        public List<UserResponse> UserList { get; set; }

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

            MetaData = new MetaData()
            {
                PageSize = 10,
                CurrentPage = 1,
                TotalPages = 0,
                TotalCount = 0
            };
            UserList = new List<UserResponse>();
            Permissions = await GetPermissionList();
        }

        public async Task Init()
        {
            //IsPreLoader = true;
            await GetContent();
        }

        public async Task OnCreateUserClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("Permissions", Permissions);
            parameters.Add("IsEdit", false);
            parameters.Add("UserModel", new UserRegisterModel());
            parameters.Add("DialogStyle", "max-width: 65rem;");

            var options = new ModalOptions();

            ModalService.Open<CreateOrUpdateModal>("Create User", parameters, options);
        }

        public async Task OnEditClickedEvent(UserResponse user)
        {
            var parameters = new ModalParameters();

            parameters.Add("IsEdit", true);
            parameters.Add("Permissions", Permissions);
            parameters.Add("UserModel", new UserRegisterModel()
            {
                Email = user.Email,
                NickName = user.NickName,
                Password = user.Password,
                Status = user.Status,
                UserName = user.UserName,
                UserId = user.UserId,
                RoleId = user.RoleId,
                RoleName = user.RoleName,
                CreatedDate = user.CreatedDate,
            });
            parameters.Add("DialogStyle", "max-width: 65rem;");

            var options = new ModalOptions();

            ModalService.Open<CreateOrUpdateModal>("Edit User", parameters, options);
        }

        public async Task ViewUserLog(string userId)
        {
            ModalParameters parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("UserId", userId);

            ModalOptions options = new ModalOptions();

            ModalService.Open<ViewUserLogModal>("View Logs", parameters, options);
        }

        public async Task OnSaveClickedEvent()
        {
            //IsPreLoader = true;
            //StateHasChanged();

            await GetContent();
        }

        protected async Task SelectedPage(int page)
        {
            MetaData.CurrentPage = page;

            //IsPreLoader = true;
            //await InvokeAsync(StateHasChanged);y

            await GetContent();
        }

        public void Dispose()
        {
            UserList.Clear();
            AuthenticationService.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        private async Task<PermissionResponse> GetPermissionList()
        {
            var response = await AuthenticationService.GetPermissionList(false);

            if (response.StatusCode == StatusCode.Success)
            {
                return new PermissionResponse
                {
                    Permissions = response.Result.Permissions ?? new List<PermissionModel>(),
                };
            }

            return new PermissionResponse();
        }

        private async Task GetContent()
        {
            IsPreLoader = true;

            var userResponse = await AuthenticationService.GetUserList(new UserGroupRequest()
            {
                MetaData = MetaData,
                SearchText = FilterModel.SearchText,
                SortBy = FilterModel.UserSorting
            });

            if (userResponse.Result != null)
            {
                MetaData = userResponse.Result.MetaData;
                UserList = userResponse.Result.Users;
            }

            if (MetaData == null)
            {
                MetaData = new MetaData()
                {
                    PageSize = 10,
                    CurrentPage = 1,
                    TotalPages = 0,
                    TotalCount = 0
                };
            }

            IsPreLoader = false;
        }

        #endregion
    }
}
