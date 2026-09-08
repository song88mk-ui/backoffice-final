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
        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }



        public MetaData MetaData { get; set; }
        public UserGroupListResponse UserGroupListModel { get; set; }
        public UserGroupCallbackModel UserGroupCallbackModel { get; set; }
        public PermissionResponse Permissions { get; set; }
        public ClientModel ClientModel { get; set; }
        public List<AlertModel> Alerts { get; set; }

        public bool IsPreLoader { get; set; }

        #endregion

        private PageHeaderModel _pageHeaderModel;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _pageHeaderModel = new()
            {
                Title = "Manage User Group",
                IconClass = "ico ico-manage-user-group"
            };
            IsPreLoader = true;
            Alerts = new List<AlertModel>();
            UserGroupListModel = new UserGroupListResponse();
            UserGroupCallbackModel = new UserGroupCallbackModel();
            Permissions = new PermissionResponse();
            ClientModel = new ClientModel();

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 0,
                TotalPages = 1,
            };

            await PageState.AuthenticateAccessEvent.InvokeAsync();

            var clients = (await BackofficeService.GetClients()).Result;

            if (clients != null && clients.Clients.Any())
            {
                ClientModel = new ClientModel
                {
                    Clients = clients.Clients.OrderBy(x => x.ClientName).ToList()
                };
            }
        }

        public async Task OnInitEvent(UserGroupCallbackModel request)
        {
            IsPreLoader = true;
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

            var userGroup = new UserGroupResponse()
            {
                UserGroupId = "",
                Claims = new List<ClaimModel> {
                    new ClaimModel()
                    {
                        ClaimType = AdminPermission.Admin_Dashboard.ToString().Replace("_","."),
                        ClaimValue = "1"
                    },
                    new ClaimModel()
                    {
                        ClaimType = SportsbookClientPermission.SBC_Dashboard.ToString().Replace("_","."),
                        ClaimValue = "1"
                    }
                }
            };

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("UserGroup", userGroup);
            parameters.Add("Permissions", Permissions);
            parameters.Add("ClientModel", ClientModel);
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
            parameters.Add("ClientModel", ClientModel);
            parameters.Add("IsEdit", true);

            var options = new ModalOptions();
            ModalService.Open<CreateOrUpdateModal>("Edit User Group", parameters, options);
        }
        public async Task OnViewUserGroupLogClickEvent(UserGroupResponse userGroup)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("UserGroupId", userGroup.UserGroupId);
            parameters.Add("DialogStyle", "min-width:900px;");

            var options = new ModalOptions();
            ModalService.Open<ViewUserGroupLogModal>("View User Group Logs", parameters, options);
        }

        public async Task OnDeleteUserGroupClickEvent(UserGroupResponse userGroup)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("UserGroup", userGroup);

            var options = new ModalOptions();
            ModalService.Open<DeleteUserGroupModal>("Delete User Group", parameters, options);
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
            parameters.Add("ModalDialogClass", "modal-lg modal-dialog-centered");
            parameters.Add("Users", users.Select((u, index) => new UserResponse { UserId = (index + 1).ToString(), UserName = u.UserName }).ToList());
            parameters.Add("UserGroupName", userGroup.UserGroupName);
            parameters.Add("ModalContentStyle", "max-height: 90vh");

            var options = new ModalOptions();
            ModalService.Open<ViewUsersModal>("View Users", parameters, options);
        }

        public async Task OnSaveClickEvent(object item)
        {
            var message = "User Group successfully created";

            if (item.ToString() == "EDIT")
                message = "User Group successfully updated";
            else if (item.ToString() == "DELETE")
                message = "User Group successfully deleted";

            Alerts.Clear();
            Alerts.Add(new AlertModel
            {
                Status = AlertStatus.Success,
                Message = message,
            });

            UserGroupListModel = await GetUserGroupList(UserGroupCallbackModel.SearchText, UserGroupCallbackModel.SortBy, MetaData.CurrentPage);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        private async Task<PermissionResponse> GetPermissionList()
        {
            var response = await AuthenticationService.GetPermissionList(false, false);

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
                SearchText = search ?? string.Empty,
                Page = page,
                PageSize = 10,
                ShowSuperAdmin = false,
            };

            var response = await AuthenticationService.GetUserGroupList(request);

            if (response.StatusCode == StatusCode.Success)
            {
                MetaData = response.Result.MetaData;

                var temp = response.Result.UserGroups;

                if (sortBy == (int)UserGroupSorting.SortByUserGroupNameAsc)
                    temp = temp.OrderBy(x => x.UserGroupName).ToList();
                if (sortBy == (int)UserGroupSorting.SortByUserGroupNameDesc)
                    temp = temp.OrderByDescending(x => x.UserGroupName).ToList();

                var userGroupList = temp.Select((s, index) => new UserGroupResponse
                {
                    Num = index + ((MetaData.CurrentPage - 1) * MetaData.PageSize) + 1,
                    UserGroupId = s.UserGroupId,
                    UserGroupName = s.UserGroupName,
                    Claims = s.Claims
                }).ToList();

                IsPreLoader = false;

                return new UserGroupListResponse
                {
                    MetaData = MetaData,
                    UserGroups = userGroupList
                };
            }

            IsPreLoader = false;

            return new UserGroupListResponse();
        }

        #endregion
    }
}
