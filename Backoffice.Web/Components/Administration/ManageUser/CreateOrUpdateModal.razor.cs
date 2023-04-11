using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Backoffice.Web.Components.Administration.ManageUser
{
    public partial class CreateOrUpdateModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        public List<AlertModel> Alerts { get; set; }
        public EditContext EditContext { get; set; }
        public Permission PermissionComponent { get; set; }
        public PermissionResponse Permissions { get; set; }
        public ClientModel ClientModel { get; set; }
        public List<UserGroupResponse> UserGroupList { get; set; }
        public UserRegisterModel UserRegister { get; set; }
        public string OldPassword { get; set; }
        public bool IsValidUsername { get; set; }
        public bool IsValidEmail { get; set; }
        public bool IsValidPassword { get; set; }
        public bool IsEdit { get; set; }
        public string PasswordErrorMessage { get; set; }
        public bool IsPreLoader { get; set; }
        #endregion

        protected override async Task OnInitializedAsync()
        {
            Alerts = new List<AlertModel>();
            ClientModel = new ClientModel();
            UserRegister = new UserRegisterModel();
            UserGroupList = new List<UserGroupResponse>();
            IsValidPassword = true;
            IsValidUsername = true;
            IsValidEmail = true;
            IsPreLoader = true;
            Permissions = ModalParameters.TryGet<PermissionResponse>("Permissions");
            IsEdit = ModalParameters.TryGet<bool>("IsEdit");
            UserRegister = ModalParameters.TryGet<UserRegisterModel>("UserModel");

            var clients = (await BackOfficeService.GetClients()).Result;

            if (clients != null && clients.Clients.Any())
            {
                ClientModel = new ClientModel
                {
                    Clients = clients.Clients.ToList()
                };
            }

            if (IsEdit)
            {
                OldPassword = UserRegister.Password;
                UserRegister.Password = "";
            }

            await GetUserGroupList();
        }
        public async Task OnPasswordFocusOutEvent()
        {
            IsValidPassword = true;
            Alerts.Clear();

            if (!IsEdit)
            {
                if (!(UserRegister.Password.Count() >= 8 && UserRegister.Password.Count() <= 15))
                {
                    IsValidPassword = false;

                    Alerts.Add(new AlertModel
                    {
                        Status = "Error",
                        Message = "Password must be between 8 to 15 character long.",
                        Opacity = 1.0m
                    });
                }
                var regex = new Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*#?&])[A-Za-z\\d@$!%*#?&].+$");

                if (!regex.IsMatch(UserRegister.Password))
                {
                    IsValidPassword = false;

                    Alerts.Add(new AlertModel
                    {
                        Status = "Error",
                        Message = "Password must be between 8 to 15 character long.",
                        Opacity = 1.0m
                    });
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(UserRegister.Password))
                {
                    if (!(UserRegister.Password.Count() >= 8 && UserRegister.Password.Count() <= 15))
                    {
                        IsValidPassword = false;

                        Alerts.Add(new AlertModel
                        {
                            Status = "Error",
                            Message = "Password must be between 8 to 15 character long.",
                            Opacity = 1.0m
                        });
                    }
                    var regex = new Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*#?&])[A-Za-z\\d@$!%*#?&].+$");

                    if (!regex.IsMatch(UserRegister.Password))
                    {
                        IsValidPassword = false;

                        Alerts.Add(new AlertModel
                        {
                            Status = "Error",
                            Message = "Password should contain uppercase and lowercase characters, with any digit from 0-9, and must include at least one special character",
                            Opacity = 1.0m
                        });
                    }
                }
            }
        }
        public async Task OnSaveClickedEvent()
        {
            Alerts.Clear();
            IsPreLoader = true;
            StateHasChanged();

            UserRegister.AppId = (int)Application.Backoffice;
            UserRegister.AppName = Application.Backoffice.ToString();

            if (IsValid(UserRegister))
            {
                var request = new UserRegisterRequest()
                {
                    AppId = (int)Application.Backoffice,
                    AppName = Application.Backoffice.ToString(),
                    UserIdRef = 0l,
                    Email = UserRegister.Email,
                    MainUserId = "",
                    NickName = UserRegister.NickName,
                    Password = string.IsNullOrWhiteSpace(UserRegister.Password) ? "" : UserRegister.Password,
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
                        ModalService.Cancel();
                    }
                    else
                    {
                        Alerts.Add(new AlertModel
                        {
                            Status = "Error",
                            Message = $"{response.Message}",
                            Opacity = 1.0m
                        });
                    }
                }
                else
                {
                    var response = (await AuthenticationService.Register(request));

                    if (response.StatusCode == StatusCode.Success)
                    {

                        await InvokeAsync(StateHasChanged);
                        await Task.Delay(3500);
                        ModalService.Cancel();

                        await Modal.OnSaveClickCallback.InvokeAsync();
                        ModalService.Cancel();
                    }
                    else
                    {
                        Alerts.Add(new AlertModel
                        {
                            Status = "Error",
                            Message = $"{response.Message}",
                            Opacity = 1.0m
                        });
                    }
                }
            }
            IsPreLoader = false;
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

            var userGroup = UserGroupList.Where(x => x.UserGroupId == UserRegister.RoleId).FirstOrDefault();

            if (userGroup.Claims.Any())
            {
                foreach (var claim in userGroup.Claims)
                {
                    var permission = Permissions.Permissions.FirstOrDefault(p => p.ClaimType == claim.ClaimType);
                    if (permission != null)
                    {
                        var index = Permissions.Permissions.IndexOf(permission);
                        Permissions.Permissions[index].ClaimValue = claim.ClaimValue;
                    }
                }
            }

            await PermissionComponent.OnGroupSelectChangedEvent();
        }
        public void OnCloseClickEvent()
        {
            ModalService.Cancel();
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
                SearchText = string.Empty,
                PageSize = -1,
                ShowSuperAdmin = false,
            })).Result.UserGroups;
            UserGroupList = response;

            if (!IsEdit)
            {
                UserRegister.RoleId = UserGroupList != null ? UserGroupList.FirstOrDefault().UserGroupId : "";
                UserRegister.RoleName = UserGroupList.Where(x => x.UserGroupId == UserRegister.RoleId).FirstOrDefault().UserGroupName;
            }

            var userGroup = UserGroupList.Where(x => x.UserGroupId == UserRegister.RoleId).FirstOrDefault();

            if (userGroup.Claims.Any())
            {
                foreach (var claim in userGroup.Claims)
                {
                    var permission = Permissions.Permissions.FirstOrDefault(p => p.ClaimType == claim.ClaimType);
                    if (permission != null)
                    {
                        permission.ClaimValue = claim.ClaimValue;
                    }
                }
            }


            IsPreLoader = false;
            StateHasChanged();
        }
        private bool IsValid(UserRegisterModel userModel)
        {
            IsValidPassword = true;
            IsValidUsername = true;
            IsValidEmail = true;

            if (string.IsNullOrWhiteSpace(userModel.UserName))
            {
                IsValidUsername = false;

                Alerts.Add(new AlertModel
                {
                    Status = "Error",
                    Message = "Username is empty.",
                    Opacity = 1.0m
                });
            }
            if (string.IsNullOrWhiteSpace(userModel.Email))
            {
                IsValidEmail = false;

                Alerts.Add(new AlertModel
                {
                    Status = "Error",
                    Message = "Email is empty.",
                    Opacity = 1.0m
                });
            }

            if (IsEdit)
            {
                if (!string.IsNullOrWhiteSpace(userModel.Password))
                {
                    if (!(UserRegister.Password.Count() >= 8 && UserRegister.Password.Count() <= 15))
                    {
                        IsValidPassword = false;

                        Alerts.Add(new AlertModel
                        {
                            Status = "Error",
                            Message = "Password must be between 8 to 15 character long.",
                            Opacity = 1.0m
                        });
                    }
                    var regex = new Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*#?&])[A-Za-z\\d@$!%*#?&].+$");

                    if (!regex.IsMatch(UserRegister.Password))
                    {
                        IsValidPassword = false;

                        Alerts.Add(new AlertModel
                        {
                            Status = "Error",
                            Message = "Password must contain any character from A-Z or a-z with any digit from 0-9.",
                            Opacity = 1.0m
                        });
                    }
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(userModel.Password))
                {
                    IsValidPassword = false;

                    Alerts.Add(new AlertModel
                    {
                        Status = "Error",
                        Message = "Password is empty.",
                        Opacity = 1.0m
                    });
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(UserRegister.Password))
                    {
                        if (!(UserRegister.Password.Count() >= 8 && UserRegister.Password.Count() <= 15))
                        {
                            IsValidPassword = false;

                            Alerts.Add(new AlertModel
                            {
                                Status = "Error",
                                Message = "Password must be between 8 to 15 character long.",
                                Opacity = 1.0m
                            });
                        }
                        var regex = new Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*#?&])[A-Za-z\\d@$!%*#?&].+$");

                        if (!regex.IsMatch(UserRegister.Password))
                        {
                            IsValidPassword = false;
                            Alerts.Add(new AlertModel
                            {
                                Status = "Error",
                                Message = "Password should contain uppercase and lowercase characters, with any digit from 0-9, and must include at least one special character",
                                Opacity = 1.0m
                            });
                        }
                    }
                }
            }

            if (IsValidPassword && IsValidUsername && IsValidEmail && IsValidPassword)
            {
                return true;
            }

            return false;
        }
        #endregion
    }
}
