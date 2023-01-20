using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Administration.ManageUserGroups;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.Administration
{
    public partial class ManageUsergroups : IDisposable
    {
        #region Properties
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        public MetaData MetaData { get; set; }
        public UserGroupListResponse UserGroupListModel { get; set; }
        public UserGroupCallbackModel UserGroupCallbackModel { get; set; }
        public PermissionResponse Permissions { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            UserGroupListModel = new UserGroupListResponse();
            UserGroupCallbackModel = new UserGroupCallbackModel();
            Permissions = new PermissionResponse();

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 20,
                TotalCount = 0,
                TotalPages = 1,
            };

            PageTitle = "Manage User Group";
            IconClass = string.Empty;
        }

        public async Task OnInitEvent(UserGroupCallbackModel request)
        {
            UserGroupCallbackModel = request;
            UserGroupListModel = await GetUserGroupList(UserGroupCallbackModel.SearchText, UserGroupCallbackModel.SortBy, 1);
        }
        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;
            UserGroupListModel = await GetUserGroupList(UserGroupCallbackModel.SearchText, UserGroupCallbackModel.SortBy, MetaData.CurrentPage);
        }

        public async Task OnCreateUserGroupClickEvent()
        {
            Permissions = await GetPermissionList();
            
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("UserGroup", new UserGroupResponse());
            parameters.Add("Permissions", Permissions);
            parameters.Add("IsEdit", false);

            var options = new ModalOptions();
            ModalService.Open<CreateOrUpdateModal>("Create User Group", parameters, options);
        }

        public async Task OnEditUserGroupClickEvent(UserGroupResponse userGroup)
        {
            Permissions = await GetPermissionList();

            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("UserGroup", userGroup);
            parameters.Add("Permissions", Permissions);
            parameters.Add("IsEdit", true);

            var options = new ModalOptions();
            ModalService.Open<CreateOrUpdateModal>("Edit User Group", parameters, options);
        }
        public async Task OnDeleteUserGroupClickEvent(UserGroupResponse userGroup)
        {
            
        }

        public async Task OnViewUsersClickEvent(UserGroupResponse userGroup)
        {
            var users = (await AuthenticationService.GetUserGroupUsers(new UserGroupRequest
            {
                UserGroupId = userGroup.UserGroupId,
                UserGroupName = userGroup.UserGroupName
            })).Result.Users;

            var parameters = new ModalParameters();

            parameters.Add("DialogStyle", "max-width: 30rem;");
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("Users", users.Select((u, index) => new UserResponse { UserId = (index + 1).ToString(), UserName = u.UserName } ).ToList());
            parameters.Add("UserGroupName", userGroup.UserGroupName);

            var options = new ModalOptions();
            ModalService.Open<ViewUsersModal>("View Users", parameters, options);
        }

        public async Task OnSaveClickEvent(object item)
        {
            UserGroupListModel = await GetUserGroupList(UserGroupCallbackModel.SearchText, UserGroupCallbackModel.SortBy, MetaData.CurrentPage);
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        private async Task<PermissionResponse> GetPermissionList()
        {
            var response = await AuthenticationService.GetPermissionList(true);

            if (response.StatusCode == StatusCode.Success)
            {
                return new PermissionResponse
                {
                    Permissions = response.Result.Permissions ?? new List<PermissionModel>(),
                };
            }

            return new PermissionResponse();
        }

        private async Task<UserGroupListResponse> GetUserGroupList(string search, int sortBy, int page)
        {
            var request = new UserGroupListRequest
            {
                SearchText = search,
                Page = page,
                PageSize = 25,
                ShowSuperAdmin = false,
            };

            var response = await AuthenticationService.GetUserGroupList(request);

            if (response.StatusCode == StatusCode.Success)
            {
                MetaData = response.Result.MetaData;

                var userGroupList = response.Result.UserGroups.Select((s, index) => new UserGroupResponse
                {
                    Num = index + 1,
                    UserGroupId = s.UserGroupId,
                    UserGroupName = s.UserGroupName,
                    Claims = s.Claims
                }).ToList();

                if (sortBy == (int)UserGroupSorting.SortByUserGroupNameAsc)
                    userGroupList = userGroupList.OrderBy(x => x.UserGroupName).ToList();
                if (sortBy == (int)UserGroupSorting.SortByUserGroupNameDesc)
                    userGroupList = userGroupList.OrderByDescending(x => x.UserGroupName).ToList();

                return new UserGroupListResponse
                {
                    MetaData = MetaData,
                    UserGroups = userGroupList
                };
            }

            return new UserGroupListResponse();
        }

        #endregion
    }
}
