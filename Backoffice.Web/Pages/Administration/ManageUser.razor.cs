using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Administration.ManageUser;
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

        

        private MetaData MetaData { get; set; }
        public PermissionResponse Permissions { get; set; }
        public ManageUserFilterModel FilterModel { get; set; } = new ManageUserFilterModel();
        public List<UserResponse> UserList { get; set; }
        public List<AlertModel> Alerts { get; set; }

        public bool IsPreLoader { get; set; }
        public decimal Opacity { get; set; }

        #endregion

        private PageHeaderModel _pageHeaderModel;

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsPreLoader = true;
            _pageHeaderModel = new()
            {
                Title = "Manage User",
                IconClass = "ico ico-manage-user"
            };
            Opacity = 0m;

            MetaData = new MetaData()
            {
                PageSize = 10,
                CurrentPage = 1,
                TotalPages = 0,
                TotalCount = 0
            };

            UserList = new List<UserResponse>();
            Alerts = new List<AlertModel>();

            await PageState.AuthenticateAccessEvent.InvokeAsync();

            Permissions = await GetPermissionList();
        }

        public async Task Init()
        {
            //IsPreLoader = true;
            MetaData.CurrentPage = 1;

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

        public async Task OnEditClickEvent(UserResponse user)
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

        public async Task OnSaveClickEvent(object item)
        {
            var itemCasted = item as Tuple<UserRegisterRequest, bool>;
            //IsPreLoader = true;
            //StateHasChanged();

            Opacity = Opacity == 1m ? 0.99m : 1m;

            string message = string.Empty;

            if (itemCasted.Item2)
            {
                message = $"<b>{itemCasted.Item1.UserName}</b> has been successfully created";
            }
            else
            {
                message = $"<b>{itemCasted.Item1.UserName}</b> has been successfully updated";
            }

            Alerts.Clear();
            Alerts.Add(new AlertModel
            {
                Status = AlertStatus.Success,
                Message = message,
                Opacity = Opacity
            });

            await GetContent();
        }

        protected async Task SelectedPage(int page)
        {
            MetaData.CurrentPage = page;

            //IsPreLoader = true;
            //await InvokeAsync(StateHasChanged);

            await GetContent();
        }

        public void Dispose()
        {
            UserList.Clear();

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
