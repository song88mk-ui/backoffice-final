using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RestSharp;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Backoffice.Web.Components.ManageUser
{
    public partial class CreateOrUpdateModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }


        public List<UserGroupResponse> UserGroupList { get; set; }
        public UserRegisterModel UserRegister { get; set; }
        public bool IsValidPassword { get; set; }
        public bool IsEdit { get; set; }
        public string PasswordErrorMessage { get; set; }
        public bool IsPreLoader { get; set; }
        #endregion

        protected override async Task OnInitializedAsync()
        {
            UserRegister = new UserRegisterModel();
            UserGroupList = new List<UserGroupResponse>();
            IsValidPassword = true;
            IsPreLoader = true;
            IsEdit = false;

            await GetUserGroupList();
        }
        public async Task OnPasswordFocusOutEvent()
        {
            IsValidPassword = true;

            if (!(UserRegister.Password.Count() >= 8 && UserRegister.Password.Count() <= 15))
            {
                IsValidPassword = false;
            }
            var regex = new Regex("^(?=.*[a-zA-Z])(?=.*[0-9]).+$");

            if (!regex.IsMatch(UserRegister.Password))
            {
                IsValidPassword = false;
            }
        }
        public async Task OnSaveClickedEvent()
        {
            UserRegister.AppId = (int)Application.Backoffice;
            UserRegister.AppName = Application.Backoffice.ToString();
            UserRegister.Status = "Normal";

            if (IsValid(UserRegister))
            {
                var request = new UserRegisterRequest()
                {
                    AppId = (int)Application.Backoffice,
                    AppName = Application.Backoffice.ToString(),
                    Email = UserRegister.Email,
                    MainUserId = "",
                    NickName = UserRegister.NickName,
                    Password = IsEdit ? "" : UserRegister.Password,
                    Status = UserRegister.Status == null ? "Normal" : UserRegister.Status,
                    UserName = UserRegister.UserName,
                    UserId = IsEdit ? UserRegister.UserId : Guid.NewGuid().ToString(),
                    RoleId = UserRegister.RoleId,
                    RoleName = UserRegister.RoleName,
                    CreatedDate = IsEdit ? UserRegister.CreatedDate : DateTime.UtcNow,
                };

                request.Claims = new List<ClaimModel>();

                if (IsEdit == true)
                {
                    var response = (await AuthenticationService.UpdateUser(request));

                    if (response.Result)
                    {
                        await InvokeAsync(StateHasChanged);
                        await Task.Delay(3500);

                        ModalService.Cancel();
                        await Modal.OnSaveClickCallback.InvokeAsync();
                    }
                    else
                    {
                        //Alerts.Add(new AlertModel
                        //{
                        //    AlertStatus = "Error",
                        //    AltertMessage = $"{response.Message}",
                        //    Opacity = 1.0m
                        //});
                    }
                }
                else
                {
                    var response = (await AuthenticationService.Register(request));

                    if (response.Result)
                    {

                        await InvokeAsync(StateHasChanged);
                        await Task.Delay(3500);
                        ModalService.Cancel();

                        await Modal.OnSaveClickCallback.InvokeAsync();
                    }
                    else
                    {
                        //Alerts.Add(new AlertModel
                        //{
                        //    AlertStatus = "Error",
                        //    AltertMessage = $"{response.Message}",
                        //    Opacity = 1.0m
                        //});
                    }
                }
            }
        }
        public async Task OnStatusClickedEvent(bool isLocked)
        {
            if (isLocked)
                UserRegister.Status = "Locked";
            else
                UserRegister.Status = "Normal";
        }
        public async Task OnUserGroupOnChangedEvent(ChangeEventArgs e)
        {
            UserRegister.RoleId = e.Value.ToString();
            UserRegister.RoleName = UserGroupList.Where(x => x.UserGroupId == UserRegister.RoleId).FirstOrDefault().UserGroupName;
        }
        #region Methods
        public void Dispose()
        {
        }
        #endregion

        #region Utilities
        private async Task GetUserGroupList()
        {
            var response = (await AuthenticationService.GetUserGroupList(new UserGroupListRequest
            {
                PageSize = -1,
                ShowSuperAdmin = false,
            })).Result.UserGroups;

            UserGroupList = response;
            if (!IsEdit)
            {
                UserRegister.RoleId = UserGroupList != null ? UserGroupList.FirstOrDefault().UserGroupId : "";
                UserRegister.RoleName = UserGroupList.Where(x => x.UserGroupId == UserRegister.RoleId).FirstOrDefault().UserGroupName;
            }

            IsPreLoader = false;
            StateHasChanged();
        }
        private bool IsValid(UserRegisterModel userModel)
        {
            if (string.IsNullOrWhiteSpace(userModel.UserName) ||
                string.IsNullOrWhiteSpace(userModel.Password) ||
                string.IsNullOrWhiteSpace(userModel.Email))
            {
                return false;
            }

            if (!IsValidPassword)
            {
                return false;
            }

            return true;
        }
        #endregion
    }
}
