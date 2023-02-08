using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.Shared.OddsEngine;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using System.Security.Claims;
using System.Text.Json;

namespace Backoffice.Web.Shared
{
    public class MainLayoutBase : LayoutComponentBase, IAsyncDisposable, IDisposable
    {
        #region Properties
        [Inject] private ToastService? _toasterService { get; set; }
        [Inject] private IHttpContextAccessor _httpContextAccessor { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IConfiguration Configuration { get; set; }
        [Inject] public IHttpContextAccessor HttpContextAccessor { get; set; }
        [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        [Parameter] public SportClickCallbackModel SelectedSport { get; set; }

        private ToastService toasterService => _toasterService!;

        private HubConnection HubConnection { get; set; }
        public HeaderNavMenu HeaderNavMenuReference { get; set; }

        public UserModel UserModel { get; set; } = new UserModel();
        public LogInModel Model { get; set; }
        public NavMenuModel NavMenus { get; set; }

        public EventCallback<SportClickCallbackModel> OnSportClickCallback { get; set; }
        //public bool IsPreLoader { get; set; } = true;
        public bool IsPreLoader { get; set; }
        public string InlineSpinnerCss { get; set; }
        public string ToastTitle { get; set; }
        public string ToastMessage { get; set; }
        public string ToastId { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                IsPreLoader = true;

                InlineSpinnerCss = "width:10rem;height:10rem;";

                if (HttpContextAccessor?.HttpContext?.Session.GetString("UserToken") == null)
                {
                    NavigationManager.NavigateTo("login", true);
                    return;
                }

                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var userClaims = authState.User.Claims.ToList();
                var role = userClaims.FirstOrDefault(c => c.Type.ToUpper() == "ROLENAME")?.Value ?? string.Empty;
                var isSuperAdmin = !string.IsNullOrEmpty(role) && role.ToUpper().Equals("SUPER ADMIN") ? true : false;

                var clients = new ClientModel();

                if (isSuperAdmin)
                    clients = (await BackOfficeService.GetClients()).Result;

                var tasks = new List<Task>();
                var otherPermissions = new List<string>() { "CLIENTCODES", "USERID", "TOKENNAME", "MAINUSERID" };

                if (userClaims is not null)
                {
                    if (isSuperAdmin)
                        UserModel.ClientCodes = clients.Clients.Select(c => c.ClientCode).ToList();
                    else
                        UserModel.ClientCodes = userClaims.Where(x => x.Type.ToUpper() == "CLIENTCODES").FirstOrDefault()?.Value.Split(',').ToList() ?? new List<string>();

                    UserModel.Id = userClaims.Where(x => x.Type.ToUpper() == "USERID").FirstOrDefault()?.Value ?? "";
                    UserModel.UserName = userClaims.Where(x => x.Type.ToUpper() == "TOKENNAME").FirstOrDefault()?.Value ?? "";
                    UserModel.MainUserId = userClaims.Where(x => x.Type.ToUpper() == "MAINUSERID").FirstOrDefault()?.Value ?? "";
                    UserModel.Claims = userClaims.Where(x => !otherPermissions.Contains(x.Type.ToUpper())).ToList();
                }

                NavMenus = new NavMenuModel();

                var NavMenuAccess = PermissionCategory.SBC;
                if (NavigationManager.Uri.Contains("/admin"))
                    NavMenuAccess = PermissionCategory.Admin;

                var permissions = await GetPermissionList();
                if (permissions != null)
                {
                    permissions.Permissions = permissions.Permissions.OrderBy(o => o.CategoryId).ThenBy(o => o.SortOrder).ToList();

                    if (UserModel != null && UserModel.Claims != null)
                    {
                        var permissionSubCategory = permissions.Permissions.Where(p => p.CategoryId == (int)NavMenuAccess && p.ClaimType != SportsbookClientPermission.SBC_Clients_Access.ToString().Replace('_', '.')).Select(s => s.SubCategoryName).Distinct();

                        foreach (var category in permissionSubCategory)
                        {
                            var subMenus = permissions.Permissions.Where(p => p.CategoryId == (int)NavMenuAccess && p.ClaimType != SportsbookClientPermission.SBC_Clients_Access.ToString().Replace('_', '.') && p.SubCategoryName == category).ToList();
                            if (subMenus.Count() == 1)
                            {
                                var claim = UserModel.Claims.FirstOrDefault(c => c.Type == subMenus[0].ClaimType);
                                if (claim != null && claim.Value != "0")
                                {
                                    if (claim.Type == SportsbookClientPermission.SBC_SubUsers.ToString().Replace("_", ".") && !string.IsNullOrWhiteSpace(UserModel?.MainUserId))
                                        continue;

                                    if (!string.IsNullOrEmpty(subMenus[0].GroupName) && subMenus[0].ClaimType.Contains(SportsbookClientPermission.SBC_OE.ToString().Replace("_", ".")))
                                        continue;

                                    var menu = await GetMenu(claim, subMenus[0].Name);
                                    NavMenus.Menus.Add(menu);
                                }
                            }
                            else
                            {
                                var categoryId = category.ToLower().Replace(" ", "-").Replace("&", "");
                                var menu = new NavMenuModel.MenuModel
                                {
                                    Id = $"sub-menu-{categoryId}",
                                    Name = category,
                                    IconName = categoryId,
                                    Url = $"#sub-menu-{categoryId}",
                                };

                                foreach (var subMenu in subMenus)
                                {
                                    var claim = UserModel.Claims.FirstOrDefault(c => c.Type == subMenu.ClaimType);
                                    if (claim != null && claim.Value != "0")
                                    {
                                        if (claim.Type == SportsbookClientPermission.SBC_SubUsers.ToString().Replace("_", ".") && !string.IsNullOrWhiteSpace(UserModel?.MainUserId))
                                            continue;


                                        if (claim.Type == SportsbookClientPermission.SBC_OE.ToString().Replace("_", "."))
                                        {
                                            var menu2 = new NavMenuModel.MenuModel
                                            {
                                                Id = $"sub-menu-odds-engine",
                                                Name = "Odds Engine",
                                                IconName = "odds-engine",
                                                Url = $"odds-engine",
                                            };
                                            NavMenus.Menus.Add(menu2);
                                            continue;
                                        }

                                        if (!string.IsNullOrEmpty(subMenu.GroupName) && subMenu.ClaimType.Contains(SportsbookClientPermission.SBC_OE.ToString().Replace("_", ".")))
                                            continue;

                                        var submenu1 = await GetMenu(claim, subMenu.Name);
                                        menu.SubMenus.Add(submenu1);
                                    }
                                }

                                if (menu.SubMenus.Any())
                                    NavMenus.Menus.Add(menu);
                            }
                        }

                    }
                }

                IsPreLoader = false;
            }
            catch (Exception)
            {
                NavigationManager.NavigateTo("login", true);
            }
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    await Subscribe();
                }
                catch (Exception ex)
                {
                    toasterService.AddToast(Toast.NewToast
                            ("Error", ex.Message));
                }
            }
        }

        public void OnSportClickEvent(SportClickCallbackModel model)
        {
            SelectedSport = model;
            HeaderNavMenuReference.ActivateMenu("monitoring");
        }

        public async ValueTask DisposeAsync()
        {
            if (HubConnection != null)
            {
                await HubConnection.StopAsync();
                await HubConnection.DisposeAsync();
            }
        }

        public void Dispose()
        {
            BackOfficeService.Dispose();
        }

        #endregion

        #region Utilities

        private async Task Subscribe()
        {
            try
            {
                var request = _httpContextAccessor.HttpContext?.Request;

                HubConnection = new HubConnectionBuilder()
                    .WithUrl(new Uri(Configuration["API:HubAPI"]))
                    .Build();

                //HubConnection.On<ConfigSettingsModel>("onconfigchange", async message =>
                //{
                //    await ProcessOnConfigChange(message);
                //});
                HubConnection.Closed += async (error) =>
                {
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    await HubConnection.StartAsync();
                };

                await HubConnection.StartAsync().ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        toasterService.AddToast(Toast.NewToast
                                ("Error Connecting to hub", "Fault in connecting."));
                    }
                    else if (task.IsCompletedSuccessfully)
                    {
                        UserModel.ConnectionId = HubConnection.ConnectionId.ToString();
                    }
                });

                HubConnection.On<string>("Notification", async message =>
                {
                    var payload = JsonSerializer.Deserialize<NotificationsModel>(message);
                    await ProcessNotification(payload);
                });

            }
            catch (Exception ex)
            {
                toasterService.AddToast(Toast.NewToast
                        ("Error Connecting to hub", ex.Message));
            }
        }
        private async Task ProcessNotification(NotificationsModel message)
        {
            if (message != null)
            {
                toasterService.AddToast(Toast.NewToast
                        (message.Page, message.Details, true));
            }
        }
        private async Task<NavMenuModel.MenuModel> GetMenu(Claim claim, string name)
        {
            var menu = new NavMenuModel.MenuModel
            {
                Id = $"menu.{claim.Type.ToLower()}",
                Name = claim.Value == "2" ? $"{name} (Readonly)" : $"{name}",
            };

            //ADMIN
            if (claim.Type == AdminPermission.Admin_Dashboard.ToString().Replace('_', '.'))
            {
                menu.IconName = "dashboard";
                menu.Url = "admin/dashboard";
            }
            else if (claim.Type == AdminPermission.Admin_ManageUsers.ToString().Replace('_', '.'))
            {
                menu.IconName = "manage-user";
                menu.Url = "admin/manage-user";
            }
            else if (claim.Type == AdminPermission.Admin_ManageUserGroups.ToString().Replace('_', '.'))
            {
                menu.IconName = "manage-user-group";
                menu.Url = "admin/manage-usergroup";
            }
            else if (claim.Type == AdminPermission.Admin_Settlements_ManualBetSettlement.ToString().Replace('_', '.'))
            {
                menu.IconName = "manual-bet-settlement";
                menu.Url = "admin/manual-bet-settlement";
            }
            else if (claim.Type == AdminPermission.Admin_Settlements_ResendSettlement.ToString().Replace('_', '.'))
            {
                menu.IconName = "resend-settlement";
                menu.Url = "admin/resend-settlement";
            }
            else if (claim.Type == AdminPermission.Admin_Settlements_MarketSettlement.ToString().Replace('_', '.'))
            {
                menu.IconName = "market-settlement";
                menu.Url = "admin/market-settlement";
            }
            else if (claim.Type == AdminPermission.Admin_Settlements_MessageSettlement.ToString().Replace('_', '.'))
            {
                menu.IconName = "message-settlement";
                menu.Url = "admin/message-settlement";
            }
            else if (claim.Type == AdminPermission.Admin_CancelBets.ToString().Replace('_', '.'))
            {
                menu.IconName = "cancel-bets";
                menu.Url = "admin/cancel-bets";
            }
            else if (claim.Type == AdminPermission.Admin_CancelBetsHistory.ToString().Replace('_', '.'))
            {
                menu.IconName = "cancel-bet-history";
                menu.Url = "admin/cancel-bet-history";
            }
            else if (claim.Type == AdminPermission.Admin_Maintenance.ToString().Replace('_', '.'))
            {
                menu.IconName = "maintenance";
                menu.Url = "admin/maintenance";
            }

            //SBC
            if (claim.Type == SportsbookClientPermission.SBC_Dashboard.ToString().Replace('_', '.'))
            {
                menu.IconName = "dashboard";
                menu.Url = "/dashboard";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_DrillDownReport.ToString().Replace('_', '.'))
            {
                menu.IconName = "drill-down-report";
                menu.Url = "/drill-down-report";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_OpenBets.ToString().Replace('_', '.'))
            {
                menu.IconName = "open-bets";
                menu.Url = "/open-bets";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_SearchBetId.ToString().Replace('_', '.'))
            {
                menu.IconName = "search-bet";
                menu.Url = "/search-bet";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_SearchUserId.ToString().Replace('_', '.'))
            {
                menu.IconName = "search-user";
                menu.Url = "/search-user";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_SearchEvent.ToString().Replace('_', '.'))
            {
                menu.IconName = "search-event";
                menu.Url = "/search-event";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_MonitorUsers.ToString().Replace('_', '.'))
            {
                menu.IconName = "monitor-users";
                menu.Url = "/monitor-users";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_Sports.ToString().Replace('_', '.'))
            {
                menu.IconName = "sports";
                menu.Url = "/sports";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_MarketReport.ToString().Replace('_', '.'))
            {
                menu.IconName = "market-report";
                menu.Url = "/market-report";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_CLTV.ToString().Replace('_', '.'))
            {
                menu.IconName = "cltv";
                menu.Url = "/cltv";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Reports_Performance.ToString().Replace('_', '.'))
            {
                menu.IconName = "performance";
                menu.Url = "/performance";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Settings_TopLeagues.ToString().Replace('_', '.'))
            {
                menu.IconName = "top-leagues";
                menu.Url = "/top-leagues";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Settings_TopFixtures.ToString().Replace('_', '.'))
            {
                menu.IconName = "top-fixtures";
                menu.Url = "/top-fixtures";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Settings_ParlayBonus.ToString().Replace('_', '.'))
            {
                menu.IconName = "parlay-bonus";
                menu.Url = "/parlay-bonus";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_SubUsers.ToString().Replace('_', '.'))
            {
                menu.IconName = "sub-user";
                menu.Url = "/sub-user";
            }
            else if (claim.Type == SportsbookClientPermission.SBC_Clients.ToString().Replace('_', '.'))
            {
                menu.IconName = "clients";
                menu.Url = "clients";
            }


            return menu;
        }

        private async Task<PermissionResponse> GetPermissionList()
        {
            var response = await AuthenticationService.GetPermissionList(false);

            if (response.StatusCode == StatusCode.Success)
            {
                return new PermissionResponse
                {
                    Permissions = response.Result.Permissions ?? new List<PermissionModel>(),
                };
            }

            return new PermissionResponse();
        }

        #endregion
    }
}
