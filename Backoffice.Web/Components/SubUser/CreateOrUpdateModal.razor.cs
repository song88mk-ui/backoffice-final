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
        public List<SubUserClaim> DefaultClaims { get; set; }
        public bool IsTogglePassword { get; set; }

        #endregion
        protected override async Task OnInitializedAsync()
        {
            try
            {
                IsTogglePassword = false;
                SubUserClaims = ModalParameters.TryGet<SubUserClaimsModel>("SubUserClaims");
                Alerts = new List<AlertModel>();
                SubUser = ModalParameters.TryGet<UserResponse>("SubUser");
                IsEdit = ModalParameters.TryGet<bool>("IsEdit");
                SubUser.Status = IsEdit ? SubUser.Status : "Normal";

                DefaultClaims = new List<SubUserClaim>();
                if(SubUserClaims != null && SubUserClaims.SubUserClaims.Any())
                {
                    foreach(var claim in SubUserClaims.SubUserClaims)
                    {
                        DefaultClaims.Add(new SubUserClaim
                        {
                            ClaimType = claim.ClaimType,
                            ClaimValue = claim.ClaimValue,
                            HasReadOnly = claim.HasReadOnly,
                            IsEnabled= claim.IsEnabled,
                            Name = claim.Name
                        });
                    }
                }

                if (SubUser.UserName.IndexOf(SubUserClaims.MainUserName) == 0)
                    SubUser.UserName = SubUser.UserName.Replace(SubUserClaims.MainUserName, "");

            }
            catch (Exception)
            {

            }
        }

        public async Task OnResetClickEvent()
        {
            SubUser = new UserResponse();
            SubUser.Status = "Normal";

            SubUserClaims.SubUserClaims = new List<SubUserClaim>();
            if (DefaultClaims.Any())
            {
                foreach (var claim in DefaultClaims)
                {
                    SubUserClaims.SubUserClaims.Add(new SubUserClaim
                    {
                        ClaimType = claim.ClaimType,
                        ClaimValue = claim.ClaimValue,
                        HasReadOnly = claim.HasReadOnly,
                        IsEnabled = claim.IsEnabled,
                        Name = claim.Name
                    });
                }
            }
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnTogglePasswordClickEvent()
        {
            IsTogglePassword = !IsTogglePassword;
        }

        public async Task OnSubUserPreferenceClicked(ChangeEventArgs e, SubUserClaim claim, bool isReadOnly)
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
            if(IsEdit)
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
                AppId = (int)Application.SportsbookClient,
                AppName = Application.SportsbookClient.ToString(),
                Email = "sbsubaccount@fci.com",
                MainUserId = SubUserClaims.MainUserId,
                NickName = SubUser.NickName,
                Password = IsEdit ? "" : SubUser.Password,
                Status = SubUser.Status,
                UserName = $"{SubUserClaims.MainUserName}{SubUser.UserName}",
                UserId = IsEdit ? SubUser.UserId : Guid.NewGuid().ToString(),
                RoleName = "Default",
                CreatedDate = IsEdit ? SubUser.CreatedDate : DateTime.UtcNow,
            };

            request.Claims = new List<ClaimModel>();

            if (SubUserClaims.SubUserClaims.Any())
            {
                foreach (var subClaim in SubUserClaims.SubUserClaims)
                {
                    request.Claims.Add(new ClaimModel { ClaimType = subClaim.ClaimType, ClaimValue = subClaim.ClaimValue });
                }
            }

            if(IsEdit == true)
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
