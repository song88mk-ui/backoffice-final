using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.SBC.SubUser;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Pages.SBC
{
    public partial class SubUser : IDisposable
    {
        #region Properties

        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        public SubUserCallbackModel CallbackModel { get; set; }
        public PermissionResponse PermissionResponse { get; set; }
        public SubUserClaimsModel SubUserClaims { get; set; }
        public UserListResponse SubUsers { get; set; }
        public MetaData MetaData { get; set; }
        public List<AlertModel> Alerts { get; set; }
        public bool IsPreload { get; set; }
        public decimal Opacity { get; set; }

        #endregion
        private PageHeaderModel _pageHeaderModel;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _ = JSRuntime.InvokeVoidAsync("console.log", "SubUser - OnInitializedAsync");

            _pageHeaderModel = new()
            {
                Title = "Sub Users",
                IconClass = "ico ico-sub-user"
            };
            Opacity = 0m;
            Alerts = new List<AlertModel>();
            PermissionResponse = new PermissionResponse();
            SubUserClaims = new SubUserClaimsModel();
            SubUsers = new UserListResponse();
            CallbackModel = new SubUserCallbackModel();
            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 1,
            };

            await PageState.AuthenticateAccessEvent.InvokeAsync();
        }

        public async Task OnInitEvent(SubUserCallbackModel model)
        {
            _ = JSRuntime.InvokeVoidAsync("console.log", "SubUser - OnInitEvent");

            CallbackModel = model;
            SubUsers = await GetSubUsers(1, model.SearchText);

            if (model.IsFirstTime)
            {
                SubUserClaims.MainUserId = _userState.Model.Id;
                SubUserClaims.MainUserName = _userState.Model.Name;
                SubUserClaims.SubUserClaims = new List<PermissionModel>();

                PermissionResponse = await GetPermissionList();

                if (PermissionResponse != null && PermissionResponse.Permissions.Any())
                {
                    SubUserClaims.SubUserClaims = GetSubUserClaims(null);
                }
            }
        }

        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;

            SubUsers = await GetSubUsers(page, CallbackModel.SearchText);
        }

        public async Task OnCreateSubUserClickEvent()
        {
            Alerts.Clear();
            SubUserClaims.SubUserClaims.Clear();
            SubUserClaims.SubUserClaims = GetSubUserClaims(null);
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("SubUserClaims", SubUserClaims);
            parameters.Add("SubUser", new UserResponse());
            parameters.Add("IsEdit", false);

            var options = new ModalOptions();
            ModalService.Open<CreateOrUpdateModal>("Create Sub User", parameters, options);
        }

        public async Task OnEditSubUserClickEvent(UserResponse user)
        {
            Alerts.Clear();
            SubUserClaims.SubUserClaims.Clear();
            SubUserClaims.SubUserClaims = GetSubUserClaims(user.Claims);
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("SubUserClaims", SubUserClaims);
            parameters.Add("SubUser", user);
            parameters.Add("IsEdit", true);

            var options = new ModalOptions();
            ModalService.Open<CreateOrUpdateModal>("Edit Sub-Users", parameters, options);
        }

        public async Task OnSaveClickEvent(object item)
        {
            Opacity = Opacity == 1m ? 0.99m : 1m;

            Alerts.Clear();
            Alerts.Add(new AlertModel
            {
                Status = AlertStatus.Success,
                Message = (bool)item ? "Sub user successfully updated" : "Sub user successfully created",
                Opacity = Opacity
            });

            SubUsers = await GetSubUsers(1, CallbackModel.SearchText);
        }

        public void Dispose()
        {
            JSRuntime.InvokeVoidAsync("console.log", "SubUser - Dispose");

            //PermissionResponse = null;
            //SubUserClaims = null;
            //SubUsers = null;
            //Alerts = null;

            //GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        private async Task<UserListResponse> GetSubUsers(int page, string searchText)
        {
            var request = new SubAccountListRequest
            {
                MainUserId = _userState.Model.Id,
                SearchText = searchText,
                Page = page,
                PageSize = 25
            };

            var response = await AuthenticationService.GetSubAccounts(request);

            if (response.StatusCode == StatusCode.Success)
            {
                MetaData = new MetaData
                {
                    CurrentPage = response.Result.MetaData.CurrentPage,
                    PageSize = response.Result.MetaData.PageSize,
                    TotalCount = response.Result.MetaData.TotalCount,
                    TotalPages = response.Result.MetaData.TotalPages,
                };

                return new UserListResponse
                {
                    MetaData = MetaData,
                    Users = response.Result.Users ?? new List<UserResponse>()
                };
            }

            return new UserListResponse();
        }

        private async Task<PermissionResponse> GetPermissionList()
        {
            var response = await AuthenticationService.GetPermissionList(true, false);

            if (response.StatusCode == StatusCode.Success)
            {
                return new PermissionResponse
                {
                    Permissions = response.Result.Permissions ?? new List<PermissionModel>(),
                };
            }

            return new PermissionResponse();
        }

        private List<PermissionModel> GetSubUserClaims(List<ClaimModel> claims)
        {
            var subUserClaims = new List<PermissionModel>();

            bool hasDashboard = false;
            foreach (var permission in PermissionResponse.Permissions.Where(x => _userState.Model.Claims?.Any(um => um.Type == x.ClaimType) ?? false))
            {
                if (permission.IsSubUser)
                {
                    var availability = 0;
                    int.TryParse(_userState.Model.Claims?.Where(x => x.Type == permission.ClaimType).FirstOrDefault()?.Value, out availability);

                    var subUserClaim = new PermissionModel
                    {
                        Id = permission.Id,
                        CategoryId = permission.CategoryId,
                        CategoryName = permission.CategoryName,
                        GroupName = permission.GroupName,
                        IsSubUser = permission.IsSubUser,
                        SortOrder = permission.SortOrder,
                        SubCategoryName = permission.SubCategoryName,
                        Name = permission.Name,
                        ClaimType = permission.ClaimType,
                        HasReadOnly = permission.HasReadOnly,
                        ClaimValue = "0",
                        Availability = availability
                    };

                    if (permission.Name.ToLower() == "dashboard")
                    {
                        hasDashboard = true;
                        subUserClaim.ClaimValue = "1";
                        subUserClaim.Availability = 1;
                    }
                    else
                    {
                        var claim = _userState.Model.Claims?.FirstOrDefault(c => c.Type == permission.ClaimType);
                        if (claim != null && Convert.ToInt32(claim.Value) > 0)
                        {
                            subUserClaim.ClaimValue = claim.Value;
                        }

                        if (string.IsNullOrEmpty(subUserClaim.ClaimValue) || subUserClaim.ClaimValue == "0")
                            subUserClaim.Availability = 0;

                        if (claims != null)
                        {
                            var userClaim = claims.FirstOrDefault(c => c.ClaimType == subUserClaim.ClaimType);
                            if (userClaim != null)
                                subUserClaim.ClaimValue = userClaim.ClaimValue;
                            else
                                subUserClaim.ClaimValue = "0";
                        }

                    }

                    subUserClaims.Add(subUserClaim);

                }
            }

            if (!hasDashboard)
            {
                var tempPermission = PermissionResponse.Permissions.FirstOrDefault(c => c.ClaimType.ToLower() == SportsbookClientPermission.SBC_Dashboard.ToString().Replace("_", ".").ToLower());
                subUserClaims.Insert(0, new PermissionModel
                {
                    CategoryId = (int)PermissionCategory.SBC,
                    CategoryName = "SportsbookClient",
                    SubCategoryName = "Dashboard",
                    GroupName = string.Empty,
                    IsSubUser = true,
                    Name = "Dashboard",
                    ClaimType = tempPermission?.ClaimType,
                    Id = tempPermission?.Id,
                    HasReadOnly = false,
                    ClaimValue = "1",
                    Availability = 0
                });
            }

            return subUserClaims;
        }

        private string GetClaimsList(List<ClaimModel> claims)
        {
            var userClaims = _userState.Model.Claims?.Where(x => claims.Any(c => c.ClaimType == x.Type && x.Value == c.ClaimValue)).Select(x => x.Type).ToList();


            return string.Join(", ", PermissionResponse.Permissions.Where(x => userClaims != null && userClaims.Contains(x.ClaimType)).Select(x => x.Name).ToList());
        }

        #endregion
    }
}
