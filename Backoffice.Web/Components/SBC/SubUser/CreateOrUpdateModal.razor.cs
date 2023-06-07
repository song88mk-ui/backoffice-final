using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.SBC.SubUser
{
    public partial class CreateOrUpdateModal : IDisposable
    {
        #region Properties

        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        public UserResponse SubUser { get; set; }
        public SubUserClaimsModel SubUserClaims { get; set; }
        public List<AlertModel> Alerts { get; set; }
        public List<PermissionModel> Permissions { get; set; }
        public ClientModel ClientModel { get; set; }

        public bool IsTogglePassword { get; set; }
        public bool IsPreload { get; set; }
        public decimal Opacity { get; set; }
        public bool IsEdit { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                IsPreload = false;
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
                            Id = claim.Id,
                            CategoryId = claim.CategoryId,
                            CategoryName = claim.CategoryName,
                            SubCategoryName = claim.SubCategoryName,
                            GroupName = claim.GroupName,
                            Availability = claim.Availability,
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
            if (IsPreload)
                return;

            IsPreload = false;
            SubUser = new UserResponse();
            SubUser.Status = "Normal";

            Permissions = new List<PermissionModel>();
            if (SubUserClaims.SubUserClaims.Any())
            {
                foreach (var claim in SubUserClaims.SubUserClaims)
                {
                    SubUserClaims.SubUserClaims.Add(new PermissionModel
                    {
                        Id = claim.Id,
                        CategoryId = claim.CategoryId,
                        CategoryName = claim.CategoryName,
                        SubCategoryName = claim.SubCategoryName,
                        GroupName = claim.GroupName,
                        Availability = claim.Availability,
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
            if (IsPreload)
                return;

            if (IsEdit)
                SubUser.UserName = $"{SubUserClaims.MainUserName}{SubUser.UserName}";

            ModalService.Cancel();
        }
        public async Task OnSaveClickEvent()
        {
            if (IsPreload)
                return;

            IsPreload = true;
            Alerts.Clear();

            Opacity = Opacity == 1m ? 0.99m : 1m;

            if (string.IsNullOrEmpty(SubUser.UserName))
            {
                Alerts.Add(new AlertModel
                {
                    Status = "Error",
                    Message = "Sub Username is required",
                    Opacity = Opacity
                });
            }

            if (string.IsNullOrEmpty(SubUser.NickName))
            {
                Alerts.Add(new AlertModel
                {
                    Status = "Error",
                    Message = "Nickname is required",
                    Opacity = Opacity
                });
            }

            if (!IsEdit && string.IsNullOrEmpty(SubUser.Password))
            {
                Alerts.Add(new AlertModel
                {
                    Status = "Error",
                    Message = "Password is required",
                    Opacity = Opacity
                });
            }
            else if (!IsEdit && (SubUser.Password.Length < 8 || SubUser.Password.Length > 15))
            {
                Alerts.Add(new AlertModel
                {
                    Status = "Error",
                    Message = "Password length must be between 8 to 15",
                    Opacity = Opacity

                });
            }

            if (Alerts.Any())
            {
                IsPreload = false;
                return;
            }

            var request = new UserRegisterRequest()
            {
                AppId = (int)Application.Backoffice,
                AppName = Application.Backoffice.ToString(),
                Email = "sbsubaccount@fci.com",
                MainUserId = SubUserClaims.MainUserId,
                NickName = SubUser.NickName,
                Password = SubUser.Password,
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
                    request.Claims.Add(new ClaimModel { ClaimType = subClaim.Id, ClaimValue = subClaim.ClaimValue });
                }
            }

            if (IsEdit == true)
            {
                var response = (await AuthenticationService.UpdateUser(request));

                if (response.Result)
                {
                    ModalService.Cancel();
                    await Modal.OnSaveClickCallback.InvokeAsync(true);
                }
                else
                {
                    Alerts.Add(new AlertModel
                    {
                        Status = "Error",
                        Message = $"{response.Message}",
                        Opacity = Opacity
                    });
                }
            }
            else
            {
                var response = (await AuthenticationService.Register(request));

                if (response.StatusCode == StatusCode.Success)
                {
                    ModalService.Cancel();

                    await Modal.OnSaveClickCallback.InvokeAsync(false);
                }
                else
                {
                    Alerts.Add(new AlertModel
                    {
                        Status = "Error",
                        Message = $"{response.Message}",
                        Opacity = Opacity
                    });
                }
            }

            IsPreload = false;
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
