using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.ActionFilter;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Backoffice.Web.Models.Shared;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Backoffice.Web.Shared
{
    public abstract class LayoutBase : LayoutComponentBase
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IHttpContextAccessor HttpContextAccessor { get; set; }
        [Inject] public IAuthenticationService AuthenticationService { get; set; }

        [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public TranslationState TranslationState { get; set; }
        [Inject] public ToastService ToasterService { get; set; }
        [Inject] public PageState PageState { get; set; }
        [Inject] public UserState UserState { get; set; }


        public UserModel UserModel { get; set; } = new UserModel();
        public NavMenuModel NavMenus { get; set; } = new NavMenuModel();
        public NavMenuParameter NavMenuParameter { get; set; }
        public bool IsPreloader { get; set; }
        public string InlineSpinnerCss { get; set; } = "width:10rem;height:10rem;";

        #endregion

        #region Fields

        private static readonly Dictionary<string, string> PermissionMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "SBC.OE.Monitoring.Outrights", string.Empty },
            { "SBC.OE.FastOdds", string.Empty },
            { "SBC.OE.Config", "/config" },
            { "SBC.OE.SportsSettings", "/sport-settings" },
            { "SBC.OE.MarketSetting.SortOrder", "/market-sort-order" },
            { "SBC.OE.MarketSetting.Group", "/market-groups" },
            { "SBC.OE.PriceBoost.Manage", "/manage-boost" },
            { "SBC.OE.PriceBoost.Settings", "/boosted-settings" }
        };
        private HubConnection _hubConnection;

        #endregion

        #region Methods

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
                    ToasterService.AddToast(Toast.NewToast("Render Error", ex.Message));
                }
            }
        }

        protected async void InitializeState()
        {
            try
            {
                TranslationState.PropertyChanged += async (s, e) => await InvokeAsync(StateHasChanged);

                if (!UserState.IsAuthenticated)
                {
                    NavigationManager.NavigateTo("login", true);
                    return;
                }

                //var authStateTask = await ((AuthStateProvider)AuthenticationStateProvider).GetAuthenticationStateAsync();
                var clientTask = await BackofficeService.GetClients();

                //await Task.WhenAll(clientTask, tradingClientTask);
                //var authState = authStateTask;

                var userClaims = UserState.ClaimsPrincipal.Claims.ToList();
                var clientResult = clientTask;

                if (clientResult.StatusCode != StatusCode.Success)
                {
                    ToasterService.AddToast(Toast.NewToast
                            ("Get Clients",
                            $"{clientResult.Message}"));

                    return;
                }

                var clients = clientResult.Result.Clients;
                var userClientCodes = new List<string>();

                if (userClaims != null)
                {
                    var otherPermissions = new List<string>() { "CLIENTCODES", "USERID", "TOKENNAME", "MAINUSERID" };

                    var roleId = userClaims.FirstOrDefault(c => c.Type.ToUpper() == "ROLEID")?.Value ?? string.Empty;
                    var role = userClaims.FirstOrDefault(c => c.Type.ToUpper() == "ROLENAME")?.Value ?? string.Empty;
                    var isSuperAdmin = !string.IsNullOrEmpty(role) && role.ToUpper().Equals("SUPER ADMIN") ? true : false;

                    if (clients.Any())
                    {
                        if (isSuperAdmin)
                            userClientCodes = clients.Select(c => c.ClientCode).ToList();
                        else
                        {
                            userClientCodes = userClaims.Where(x => x.Type.ToUpper() == "CLIENTCODES").FirstOrDefault()?.Value.Split(',').ToList() ?? new List<string>();
                            clients = clients.Where(c => userClientCodes.Contains(c.ClientCode.ToUpper())).ToList();
                        }
                    }

                    UserModel.Id = userClaims.Where(x => x.Type.ToUpper() == "USERID").FirstOrDefault()?.Value ?? "";
                    UserModel.UserName = userClaims.Where(x => x.Type.ToUpper() == "TOKENNAME").FirstOrDefault()?.Value ?? "";
                    UserModel.MainUserId = userClaims.Where(x => x.Type.ToUpper() == "MAINUSERID").FirstOrDefault()?.Value ?? "";
                    UserModel.Claims = userClaims.Where(x => !otherPermissions.Contains(x.Type.ToUpper())).ToList();
                    UserModel.ClientCodes = userClientCodes;
                    UserModel.RoleId = roleId;
                    UserModel.RoleName = role;
                }

                NavMenus = new NavMenuModel();

                var navMenuAccess = PermissionCategory.SBC;
                if (NavigationManager.Uri.Contains("/admin"))
                    navMenuAccess = PermissionCategory.Admin;

                var permissions = await GetPermissionList();
                if (permissions == null || permissions.StatusCode != StatusCode.Success)
                {
                    NavigationManager.NavigateTo("login", true);
                    return;
                }

                var page = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "");
                if (userClaims != null)
                {
                    var permissionSubCategory = permissions.Permissions.Where(p => p.CategoryId == (int)navMenuAccess && p.ClaimType != SportsbookClientPermission.SBC_Clients_Access.ToString().Replace('_', '.')).Select(s => s.SubCategoryName).Distinct();
                    //// Task: Temporary Hide Settlement
                    //var permissionSubCategory = permissions.Permissions.Where(p => p.CategoryId == (int)NavMenuAccess && p.ClaimType != SportsbookClientPermission.SBC_Clients_Access.ToString().Replace('_', '.')
                    //&& p.SubCategoryName.ToUpper() != "Settlements".ToUpper()).Select(s => s.SubCategoryName).Distinct();

                    foreach (var category in permissionSubCategory)
                    {
                        var subMenus = permissions.Permissions.Where(p => p.CategoryId == (int)navMenuAccess && p.ClaimType != SportsbookClientPermission.SBC_Clients_Access.ToString().Replace('_', '.') && p.SubCategoryName == category).ToList();

                        if (subMenus.Count() == 1)
                        {
                            var claim = userClaims.FirstOrDefault(c => c.Type == subMenus[0].ClaimType);
                            if (claim != null && claim.Value != "0")
                            {
                                if (claim.Type == SportsbookClientPermission.SBC_SubUsers.ToString().Replace("_", ".") && !string.IsNullOrWhiteSpace(UserModel.MainUserId))
                                    continue;

                                if (!string.IsNullOrEmpty(subMenus[0].GroupName) && subMenus[0].ClaimType.Contains(SportsbookClientPermission.SBC_OE.ToString().Replace("_", ".")))
                                    continue;

                                var menu = MenuHelper.GetMenu(claim, subMenus[0].Name);
                                NavMenus.Menus.Add(menu);
                            }
                        }
                        else
                        {
                            var categoryId = category.ToLower().Replace(" ", "-").Replace("&", "");
                            var menu = new MenuModel
                            {
                                Id = $"sub-menu-{categoryId}",
                                Name = category,
                                IconName = categoryId,
                                Url = $"#sub-menu-{categoryId}",
                            };

                            if (category == "User & User Groups")
                            {
                                menu.IconName = "manage-user-group";
                            }

                            foreach (var subMenu in subMenus)
                            {
                                var claim = userClaims.FirstOrDefault(c => c.Type == subMenu.ClaimType);
                                if (claim != null && claim.Value != "0")
                                {
                                    if (claim.Type == SportsbookClientPermission.SBC_SubUsers.ToString().Replace("_", ".") && !string.IsNullOrWhiteSpace(UserModel.MainUserId))
                                        continue;

                                    if (claim.Type == SportsbookClientPermission.SBC_OE.ToString().Replace("_", "."))
                                    {
                                        var sbsFirstClaim = userClaims.FirstOrDefault(x => x.Type.ToUpper().Contains($"{SportsbookClientPermission.SBC_OE.ToString().Replace("_", ".").ToUpper()}."))?.Type.ToString() ?? string.Empty;

                                        var permission = PermissionMap.TryGetValue(sbsFirstClaim, out string permissionMapResult) ? permissionMapResult : string.Empty;
                                        var menu2 = new MenuModel
                                        {
                                            Id = $"sub-menu-odds-engine",
                                            Name = "Odds Engine",
                                            IconName = "odds-engine",
                                            Url = $"#sub-menu-odds-engine",
                                            SubMenus = new List<MenuModel>()
                                            {
                                                new MenuModel()
                                                {
                                                    Id = $"menu.odds-engine.hk",
                                                    Name = "Asia",
                                                    IconName = "location",
                                                    Url = $"hk/odds-engine{permission}",
                                                }
                                            }
                                        };

                                        NavMenus.Menus.Add(menu2);
                                        continue;
                                    }

                                    if (!string.IsNullOrEmpty(subMenu.GroupName) && subMenu.ClaimType.Contains(SportsbookClientPermission.SBC_OE.ToString().Replace("_", ".")))
                                        continue;

                                    var submenu1 = MenuHelper.GetMenu(claim, subMenu.Name);
                                    menu.SubMenus.Add(submenu1);
                                }
                            }

                            if (menu.SubMenus.Any())
                                NavMenus.Menus.Add(menu);
                        }
                    }
                }

                NavMenuParameter = new NavMenuParameter()
                {
                    NavMenuAccess = page.Contains("/admin") ? PermissionCategory.Admin : PermissionCategory.SBC,
                    NavMenus = NavMenus
                };

                var splitVal = page.Split("/").FirstOrDefault();

                if (splitVal != null)
                {
                    var regions = new List<string>() { "HK", "SA" };
                    PageState.Region = regions.Contains(splitVal.Trim().ToUpper()) ? splitVal.Trim().ToUpper() : string.Empty;
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                NavigationManager.NavigateTo("login", true);
            }
        }

        #endregion

        #region Private Methods

        private async Task Subscribe()
        {
            try
            {
                var request = HttpContextAccessor.HttpContext?.Request;
                Log.Information($"Hubconnection URI - {NavigationManager.BaseUri}");

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(new Uri(Path.Combine(NavigationManager.BaseUri, "bohub")))
                .Build();

                _hubConnection.On<NotificationsModel>("Notification", async message =>
                {
                    await ProcessNotification(message);
                });

                _hubConnection.Closed += async (error) =>
                {
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    await _hubConnection.StartAsync();
                };

                await _hubConnection.StartAsync().ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        ToasterService.AddToast(Toast.NewToast
                                ("Error Connecting to hub", "Fault in connecting."));
                    }
                    else if (task.IsCompletedSuccessfully)
                    {
                        UserModel.ConnectionId = _hubConnection?.ConnectionId?.ToString() ?? string.Empty;
                    }
                });

                //await _hubConnection.InvokeAsync("AddToGroup", "HK");
            }
            catch (Exception ex)
            {
                ToasterService.AddToast(Toast.NewToast
                        ("Error Connecting to hub", ex.Message));
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }
        }
        private async Task ProcessNotification(NotificationsModel message)
        {
            if (message != null)
            {
                if (!string.IsNullOrEmpty(message.UserId) && message.UserId != UserModel.Id)
                    return;

                ToasterService.AddToast(Toast.NewToast
                        (message.Page, message.Details, true));
            }
        }
        private async Task<PermissionResponse> GetPermissionList()
        {
            var response = await AuthenticationService.GetPermissionList(false, true);
            var permissions = new List<PermissionModel>();

            if (response.StatusCode == StatusCode.Success)
            {
                permissions = response.Result.Permissions ?? new List<PermissionModel>();
            }

            return new PermissionResponse
            {
                StatusCode = response.StatusCode,
                Permissions = permissions,
            };
        }

        #endregion
    }
}
