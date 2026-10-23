using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Text.RegularExpressions;

namespace Backoffice.Web.Components.Administration.ManageUser
{
    public partial class CreateOrUpdateModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

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

            var clients = (await BackofficeService.GetClients()).Result;

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
            var passwordErrorCount = 0;

            if (!IsEdit)
            {
                if (!(UserRegister.Password.Count() >= 8 && UserRegister.Password.Count() <= 15))
                {
                    IsValidPassword = false;
                    passwordErrorCount++;

                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Password must be between 8 to 15 character long.",
                        Opacity = 1.0m
                    });
                }

                if (!UserRegister.Password.Any(char.IsLower))
                {
                    IsValidPassword = false;
                    passwordErrorCount++;

                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Password should contain lowercase characters.",
                        Opacity = 1.0m
                    });
                }
                if (!UserRegister.Password.Any(char.IsUpper))
                {
                    IsValidPassword = false;
                    passwordErrorCount++;

                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Password should contain uppercase characters.",
                        Opacity = 1.0m
                    });
                }
                if (!UserRegister.Password.Any(char.IsDigit))
                {
                    IsValidPassword = false;
                    passwordErrorCount++;

                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Password should contain any digit from 0-9.",
                        Opacity = 1.0m
                    });
                }

                var regex = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");
                if (!regex.IsMatch(UserRegister.Password))
                {
                    IsValidPassword = false;
                    passwordErrorCount++;

                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Password must include at least one special character.",
                        Opacity = 1.0m
                    });
                }

                if (UserRegister.Password.Any(char.IsWhiteSpace))
                {
                    IsValidPassword = false;
                    passwordErrorCount++;

                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Password should not contain spaces.",
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
                        passwordErrorCount++;

                        Alerts.Add(new AlertModel
                        {
                            Status = AlertStatus.Error,
                            Message = "Password must be between 8 to 15 character long.",
                            Opacity = 1.0m
                        });
                    }

                    if (!UserRegister.Password.Any(char.IsLower))
                    {
                        IsValidPassword = false;
                        passwordErrorCount++;

                        Alerts.Add(new()
                        {
                            Status = AlertStatus.Error,
                            Message = "Password should contain lowercase characters."
                        });
                    }
                    if (!UserRegister.Password.Any(char.IsUpper))
                    {
                        IsValidPassword = false;
                        passwordErrorCount++;

                        Alerts.Add(new()
                        {
                            Status = AlertStatus.Error,
                            Message = "Password should contain uppercase characters."
                        });
                    }
                    if (!UserRegister.Password.Any(char.IsDigit))
                    {
                        IsValidPassword = false;
                        passwordErrorCount++;

                        Alerts.Add(new()
                        {
                            Status = AlertStatus.Error,
                            Message = "Password should contain any digit from 0-9."
                        });
                    }

                    var regex = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");
                    if (!regex.IsMatch(UserRegister.Password))
                    {
                        IsValidPassword = false;
                        passwordErrorCount++;

                        Alerts.Add(new()
                        {
                            Status = AlertStatus.Error,
                            Message = "Password must include at least one special character."
                        });
                    }

                    if (UserRegister.Password.Any(char.IsWhiteSpace))
                    {
                        IsValidPassword = false;
                        passwordErrorCount++;

                        Alerts.Add(new()
                        {
                            Status = AlertStatus.Error,
                            Message = "Password should not contain spaces."
                        });
                    }
                }
            }
        }
        public async Task OnSaveClickedEvent()
        {
            Alerts.Clear();
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);

            UserRegister.AppId = (int)Application2.BO;
            UserRegister.AppName = Application2.BO.ToString();

            if (IsValid(UserRegister))
            {
                var request = new UserRegisterRequest()
                {
                    AppId = (int)Application2.BO,
                    AppName = Application2.BO.ToString(),
                    UserIdRef = 0L,
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

                    if (response.StatusCode == StatusCode.Success)
                    {
                        await InvokeAsync(StateHasChanged);
                        await Task.Delay(3500);

                        await Modal.OnSaveClickCallback.InvokeAsync(new Tuple<UserRegisterRequest, bool>(request, false));
                        ModalService.Cancel();
                    }
                    else
                    {
                        Alerts.Add(new AlertModel
                        {
                            Status = AlertStatus.Error,
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

                        await Modal.OnSaveClickCallback.InvokeAsync(new Tuple<UserRegisterRequest, bool>(request, false));
                        ModalService.Cancel();
                    }
                    else
                    {
                        Alerts.Add(new AlertModel
                        {
                            Status = AlertStatus.Error,
                            Message = $"{response.Message}",
                            Opacity = 1.0m
                        });
                    }
                }
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("scrollToTopOfElement", ".modal", "fast");
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
        public void RemoveError(string property)
        {
            switch (property.ToLower())
            {
                case "username":
                    IsValidUsername = true;
                    break;
                case "email":
                    IsValidEmail = true;
                    break;
                case "password":
                    IsValidPassword = true;
                    break;
            }
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
                UserRegister.RoleId = UserGroupList != null ? UserGroupList.FirstOrDefault(x => x.UserGroupName == "Default").UserGroupId : "";
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
            var passwordErrorCount = 0;

            if (string.IsNullOrWhiteSpace(userModel.UserName))
            {
                IsValidUsername = false;

                Alerts.Add(new()
                {
                    Status = AlertStatus.Error,
                    Message = "Username is empty."
                });
            }

            if (IsEdit)
            {
                if (!string.IsNullOrWhiteSpace(userModel.Password))
                {
                    if (!(UserRegister.Password.Count() >= 8 && UserRegister.Password.Count() <= 15))
                    {
                        IsValidPassword = false;
                        passwordErrorCount++;

                        Alerts.Add(new()
                        {
                            Status = AlertStatus.Error,
                            Message = "Password must be between 8 to 15 character long."
                        });
                    }

                    if (!UserRegister.Password.Any(char.IsLower))
                    {
                        IsValidPassword = false;
                        passwordErrorCount++;

                        Alerts.Add(new()
                        {
                            Status = AlertStatus.Error,
                            Message = "Password should contain lowercase characters."
                        });
                    }
                    if (!UserRegister.Password.Any(char.IsUpper))
                    {
                        IsValidPassword = false;
                        passwordErrorCount++;

                        Alerts.Add(new()
                        {
                            Status = AlertStatus.Error,
                            Message = "Password should contain uppercase characters."
                        });
                    }
                    if (!UserRegister.Password.Any(char.IsDigit))
                    {
                        IsValidPassword = false;
                        passwordErrorCount++;

                        Alerts.Add(new()
                        {
                            Status = AlertStatus.Error,
                            Message = "Password should contain any digit from 0-9."
                        });
                    }

                    var regex = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");
                    if (!regex.IsMatch(UserRegister.Password))
                    {
                        IsValidPassword = false;
                        passwordErrorCount++;

                        Alerts.Add(new()
                        {
                            Status = AlertStatus.Error,
                            Message = "Password must include at least one special character."
                        });
                    }

                    if (UserRegister.Password.Any(char.IsWhiteSpace))
                    {
                        IsValidPassword = false;
                        passwordErrorCount++;

                        Alerts.Add(new()
                        {
                            Status = AlertStatus.Error,
                            Message = "Password should not contain spaces."
                        });
                    }
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(userModel.Password))
                {
                    IsValidPassword = false;
                    passwordErrorCount++;

                    Alerts.Add(new()
                    {
                        Status = AlertStatus.Error,
                        Message = "Password is empty."
                    });
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(UserRegister.Password))
                    {
                        if (!(UserRegister.Password.Count() >= 8 && UserRegister.Password.Count() <= 15))
                        {
                            IsValidPassword = false;
                            passwordErrorCount++;

                            Alerts.Add(new()
                            {
                                Status = AlertStatus.Error,
                                Message = "Password must be between 8 to 15 character long."
                            });
                        }

                        if (!UserRegister.Password.Any(char.IsLower))
                        {
                            IsValidPassword = false;
                            passwordErrorCount++;

                            Alerts.Add(new()
                            {
                                Status = AlertStatus.Error,
                                Message = "Password should contain lowercase characters."
                            });
                        }
                        if (!UserRegister.Password.Any(char.IsUpper))
                        {
                            IsValidPassword = false;
                            passwordErrorCount++;

                            Alerts.Add(new()
                            {
                                Status = AlertStatus.Error,
                                Message = "Password should contain uppercase characters."
                            });
                        }
                        if (!UserRegister.Password.Any(char.IsDigit))
                        {
                            IsValidPassword = false;
                            passwordErrorCount++;

                            Alerts.Add(new()
                            {
                                Status = AlertStatus.Error,
                                Message = "Password should contain any digit from 0-9."
                            });
                        }

                        var regex = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");
                        if (!regex.IsMatch(UserRegister.Password))
                        {
                            IsValidPassword = false;
                            passwordErrorCount++;

                            Alerts.Add(new()
                            {
                                Status = AlertStatus.Error,
                                Message = "Password must include at least one special character."
                            });
                        }

                        if (UserRegister.Password.Any(char.IsWhiteSpace))
                        {
                            IsValidPassword = false;
                            passwordErrorCount++;

                            Alerts.Add(new()
                            {
                                Status = AlertStatus.Error,
                                Message = "Password should not contain spaces."
                            });
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(userModel.Email))
            {
                IsValidEmail = Regex.IsMatch(userModel.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

                if (!IsValidEmail)
                {
                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Email is invalid",
                        Opacity = 1.0m
                    });
                }
            }

            if (passwordErrorCount > 1)
            {
                Alerts.RemoveAll(x => x.Message.Contains("Password"));

                Alerts.Add(new AlertModel
                {
                    Status = AlertStatus.Error,
                    //Message = "Password must be 8-15 characters long, which should contain uppercase and lowercase characters, with any digit from 0-9, and must include at least one special character.",
                    Message = "password_criteria",
                    Opacity = 1.0m
                });
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
