using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.SubUser
{
    public partial class CreateOrUpdateModal : IDisposable
    {
        #region Properties
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        public UserResponse SubUser { get; set; }
        public bool IsEdit { get; set; }
        public SubUserClaimsModel SubUserClaims { get; set; }
        public List<AlertModel> Alerts { get; set; }
        public List<PermissionModel> Permissions { get; set; }
        public ClientModel ClientModel { get; set; }
        public bool IsTogglePassword { get; set; }

        #endregion
        protected override async Task OnInitializedAsync()
        {
            try
            {
                IsTogglePassword = false;
                Permissions = new List<PermissionModel>();
                SubUserClaims = ModalParameters.TryGet<SubUserClaimsModel>("SubUserClaims");
                Alerts = new List<AlertModel>();
                SubUser = ModalParameters.TryGet<UserResponse>("SubUser");
                IsEdit = ModalParameters.TryGet<bool>("IsEdit");
                SubUser.Status = IsEdit ? SubUser.Status : "Normal";
                ClientModel = new ClientModel();

                if (SubUserClaims != null && SubUserClaims.SubUserClaims.Any())
                {
                    foreach (var claim in SubUserClaims.SubUserClaims)
                    {
                        Permissions.Add(new PermissionModel
                        {
                            CategoryId = claim.CategoryId,
                            CategoryName = claim.CategoryName,
                            SubCategoryName = claim.SubCategoryName,
                            GroupName = claim.GroupName,
                            IsEnabled = claim.IsEnabled,
                            IsSubUser = claim.IsSubUser,
                            SortOrder = claim.SortOrder,
                            ClaimType = claim.ClaimType,
                            ClaimValue = claim.ClaimValue,
                            HasReadOnly = claim.HasReadOnly,
                            Name = claim.Name,
                        });
                    }
                }

                if (!string.IsNullOrEmpty(SubUser.UserName))
                {
                    if (SubUser.UserName.IndexOf(SubUserClaims.MainUserName) == 0)
                        SubUser.UserName = SubUser.UserName.Replace(SubUserClaims.MainUserName, "");
                }
                else
                {
                    SubUser.UserName = string.Empty;
                }


            }
            catch (Exception)
            {

            }
        }

        public async Task OnResetClickEvent()
        {
            SubUser = new UserResponse();
            SubUser.Status = "Normal";

            Permissions = new List<PermissionModel>();
            if (SubUserClaims.SubUserClaims.Any())
            {
                foreach (var claim in SubUserClaims.SubUserClaims)
                {
                    SubUserClaims.SubUserClaims.Add(new PermissionModel
                    {
                        CategoryId = claim.CategoryId,
                        CategoryName = claim.CategoryName,
                        SubCategoryName = claim.SubCategoryName,
                        GroupName = claim.GroupName,
                        IsEnabled = claim.IsEnabled,
                        IsSubUser = claim.IsSubUser,
                        SortOrder = claim.SortOrder,
                        ClaimType = claim.ClaimType,
                        ClaimValue = claim.ClaimValue,
                        HasReadOnly = claim.HasReadOnly,
                        Name = claim.Name,
                    });
                }
            }
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnTogglePasswordClickEvent()
        {
            IsTogglePassword = !IsTogglePassword;
        }

        public async Task OnSubUserPreferenceClicked(ChangeEventArgs e, PermissionModel claim, bool isReadOnly)
        {
            if (Convert.ToBoolean(e.Value) == true)
            {
                if (isReadOnly == true)
                {
                    claim.ClaimValue = "2";
                }
                else
                {
                    claim.ClaimValue = "1";
                }
            }
            else
            {
                claim.ClaimValue = "0";
            }
        }
        public async Task OnStatusNormalChangedEvent(ChangeEventArgs e)
        {
            SubUser.Status = "Normal";
        }
        public async Task OnStatusLockedChangedEvent(ChangeEventArgs e)
        {
            SubUser.Status = "Locked";
        }

        public async Task OnCancelClickEvent()
        {
            if (IsEdit)
                SubUser.UserName = $"{SubUserClaims.MainUserName}{SubUser.UserName}";

            ModalService.Cancel();
        }
        public async Task OnSaveClickEvent()
        {
            Alerts.Clear();

            if (string.IsNullOrEmpty(SubUser.UserName))
            {
                Alerts.Add(new AlertModel
                {
                    AlertStatus = "Error",
                    AltertMessage = "Sub Username is required",
                    Opacity = 1.0m
                });
            }

            if (string.IsNullOrEmpty(SubUser.NickName))
            {
                Alerts.Add(new AlertModel
                {
                    AlertStatus = "Error",
                    AltertMessage = "Nickname is required",
                    Opacity = 1.0m
                });
            }

            if (!IsEdit && string.IsNullOrEmpty(SubUser.Password))
            {
                Alerts.Add(new AlertModel
                {
                    AlertStatus = "Error",
                    AltertMessage = "Password is required",
                    Opacity = 1.0m
                });
            }
            else if (!IsEdit && (SubUser.Password.Length < 8 || SubUser.Password.Length > 15))
            {
                Alerts.Add(new AlertModel
                {
                    AlertStatus = "Error",
                    AltertMessage = "Password length must be between 8 to 15",
                    Opacity = 1.0m

                });
            }

            if (Alerts.Any())
                return;

            var request = new UserRegisterRequest()
            {
                AppId = (int)Application.Backoffice,
                AppName = Application.Backoffice.ToString(),
                Email = "sbsubaccount@fci.com",
                MainUserId = SubUserClaims.MainUserId,
                NickName = SubUser.NickName,
                Password = IsEdit ? "" : SubUser.Password,
                Status = SubUser.Status,
                UserName = $"{SubUserClaims.MainUserName}{SubUser.UserName}",
                UserId = IsEdit ? SubUser.UserId : Guid.NewGuid().ToString(),
                RoleId = string.Empty,
                RoleName = "Default",
                CreatedDate = IsEdit ? SubUser.CreatedDate : DateTime.UtcNow,
            };

            request.Claims = new List<ClaimModel>();

            if (Permissions.Any())
            {
                foreach (var subClaim in Permissions)
                {
                    request.Claims.Add(new ClaimModel { ClaimType = subClaim.ClaimType, ClaimValue = subClaim.ClaimValue });
                }
            }

            if (IsEdit == true)
            {
                var response = (await AuthenticationService.UpdateUser(request));

                if (response.Result)
                {
                    Alerts.Add(new AlertModel
                    {
                        AlertStatus = "Success",
                        AltertMessage = "Sub user successfully updated",
                        Opacity = 1.0m

                    });

                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(3500);

                    ModalService.Cancel();
                    await Modal.OnSaveClickCallback.InvokeAsync();
                }
                else
                {
                    Alerts.Add(new AlertModel
                    {
                        AlertStatus = "Error",
                        AltertMessage = $"{response.Message}",
                        Opacity = 1.0m
                    });
                }
            }
            else
            {
                var response = (await AuthenticationService.Register(request));

                if (response.Result)
                {

                    Alerts.Add(new AlertModel
                    {
                        AlertStatus = "Success",
                        AltertMessage = "Sub user successfully created",
                        Opacity = 1.0m

                    });

                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(3500);
                    ModalService.Cancel();

                    await Modal.OnSaveClickCallback.InvokeAsync();
                }
                else
                {
                    Alerts.Add(new AlertModel
                    {
                        AlertStatus = "Error",
                        AltertMessage = $"{response.Message}",
                        Opacity = 1.0m
                    });
                }
            }

        }
        public void Dispose()
        {

        }
    }
}
