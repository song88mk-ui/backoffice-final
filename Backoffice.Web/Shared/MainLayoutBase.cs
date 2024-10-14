using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.ActionFilter;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.Shared.OddsEngine;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Serilog;
using System.Security.Claims;

namespace Backoffice.Web.Shared
{
    public class MainLayoutBase : LayoutComponentBase, IAsyncDisposable, IDisposable
    {
        #region Properties
        [Inject] private ToastService? _toasterService { get; set; }
        [Inject] private IHttpContextAccessor _httpContextAccessor { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IConfiguration Configuration { get; set; }
        [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        [Inject] public IPreRenderService PreRenderService { get; set; }
        [Inject] public TranslationState TranslationState { get; set; }

        public SportClickCallbackModel SelectedSport { get; set; }

        private ToastService toasterService => _toasterService!;

        private HubConnection _hubConnection { get; set; }
        public HeaderNavMenu HeaderNavMenuReference { get; set; }

        public UserModel UserModel { get; set; } = new UserModel();
        public List<Claim>? UserClaims { get; set; } = new List<Claim>();
        public List<string>? UserClientCodes { get; set; } = new List<string>();
        public string Region { get; set; } = null;
        public LogInModel Model { get; set; }
        public NavMenuModel NavMenus { get; set; }

        public static Func<Task> OnFilterClientActionAsync;
        public ClientModel Clients { get; set; } = new ClientModel();
        public EventCallback<SportClickCallbackModel> OnSportClickCallback { get; set; }
        public EventCallback AuthenticateAccessEvent => EventCallback.Factory.Create(this, AuthenticateAccess);
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
            if (PreRenderService.IsPreRendering)
                return;

            try
            {
                IsPreLoader = true;

                InlineSpinnerCss = "width:10rem;height:10rem;";

                if (_httpContextAccessor?.HttpContext?.Session.GetString("UserToken") == null)
                {
                    NavigationManager.NavigateTo("login", true);
                    IsPreLoader = false;
                    return;
                }

                var authState = await ((AuthStateProvider)AuthenticationStateProvider).GetAuthenticationStateAsync();

                var userClaims = authState.User.Claims.ToList();
                var roleId = userClaims.FirstOrDefault(c => c.Type.ToUpper() == "ROLEID")?.Value ?? string.Empty;
                var role = userClaims.FirstOrDefault(c => c.Type.ToUpper() == "ROLENAME")?.Value ?? string.Empty;
                var isSuperAdmin = !string.IsNullOrEmpty(role) && role.ToUpper().Equals("SUPER ADMIN") ? true : false;

                OnFilterClientActionAsync = OnFilterClientEvent;

                var clientResult = await BackofficeService.GetClients();

                if (clientResult.StatusCode != StatusCode.Success)
                {
                    _toasterService.AddToast(Toast.NewToast
                            ("Get Clients",
                            $"{clientResult.Message}"));
                    IsPreLoader = false;
                    return;
                }

                Clients = clientResult.Result;
                var tasks = new List<Task>();
                var otherPermissions = new List<string>() { "CLIENTCODES", "USERID", "TOKENNAME", "MAINUSERID" };

                if (userClaims is not null)
                {
                    if (Clients.Clients.Any())
                    {
                        if (isSuperAdmin)
                            UserClientCodes = Clients.Clients.Select(c => c.ClientCode).ToList();
                        else
                        {
                            UserClientCodes = userClaims.Where(x => x.Type.ToUpper() == "CLIENTCODES").FirstOrDefault()?.Value.Split(',').ToList() ?? new List<string>();
                            Clients.Clients = Clients.Clients.Where(c => UserClientCodes.Contains(c.ClientCode.ToUpper())).ToList();
                        }
                    }

                    UserModel.Id = userClaims.Where(x => x.Type.ToUpper() == "USERID").FirstOrDefault()?.Value ?? "";
                    UserModel.UserName = userClaims.Where(x => x.Type.ToUpper() == "TOKENNAME").FirstOrDefault()?.Value ?? "";
                    UserModel.MainUserId = userClaims.Where(x => x.Type.ToUpper() == "MAINUSERID").FirstOrDefault()?.Value ?? "";
                    UserClaims = userClaims.Where(x => !otherPermissions.Contains(x.Type.ToUpper())).ToList();
                    UserModel.RoleId = roleId;
                    UserModel.RoleName = role;
                }

                NavMenus = new NavMenuModel();

                var NavMenuAccess = PermissionCategory.SBC;
                if (NavigationManager.Uri.Contains("/admin"))
                    NavMenuAccess = PermissionCategory.Admin;

                var permissions = await GetPermissionList();

                if (permissions == null || permissions.StatusCode != StatusCode.Success)
                {
                    NavigationManager.NavigateTo("login", true);
                    return;
                }

                //Token = CommonService.GenerateToken();

                permissions.Permissions = permissions.Permissions.OrderBy(o => o.CategoryId).ThenBy(o => o.SortOrder).ToList();

                if (UserClaims != null)
                {
                    var permissionSubCategory = permissions.Permissions.Where(p => p.CategoryId == (int)NavMenuAccess && p.ClaimType != SportsbookClientPermission.SBC_Clients_Access.ToString().Replace('_', '.')).Select(s => s.SubCategoryName).Distinct();
                    //// Temporary Hide Settlement
                    //var permissionSubCategory = permissions.Permissions.Where(p => p.CategoryId == (int)NavMenuAccess && p.ClaimType != SportsbookClientPermission.SBC_Clients_Access.ToString().Replace('_', '.')
                    //&& p.SubCategoryName.ToUpper() != "Settlements".ToUpper()).Select(s => s.SubCategoryName).Distinct();

                    foreach (var category in permissionSubCategory)
                    {
                        var subMenus = permissions.Permissions.Where(p => p.CategoryId == (int)NavMenuAccess && p.ClaimType != SportsbookClientPermission.SBC_Clients_Access.ToString().Replace('_', '.') && p.SubCategoryName == category).ToList();

                        if (subMenus.Count() == 1)
                        {
                            var claim = UserClaims.FirstOrDefault(c => c.Type == subMenus[0].ClaimType);
                            if (claim != null && claim.Value != "0")
                            {
                                if (claim.Type == SportsbookClientPermission.SBC_SubUsers.ToString().Replace("_", ".") && !string.IsNullOrWhiteSpace(UserModel?.MainUserId))
                                    continue;

                                if (!string.IsNullOrEmpty(subMenus[0].GroupName) && subMenus[0].ClaimType.Contains(SportsbookClientPermission.SBC_OE.ToString().Replace("_", ".")))
                                    continue;

                                var menu = await MenuHelper.GetMenu(claim, subMenus[0].Name);
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
                                var claim = UserClaims.FirstOrDefault(c => c.Type == subMenu.ClaimType);
                                if (claim != null && claim.Value != "0")
                                {
                                    if (claim.Type == SportsbookClientPermission.SBC_SubUsers.ToString().Replace("_", ".") && !string.IsNullOrWhiteSpace(UserModel?.MainUserId))
                                        continue;

                                    if (claim.Type == SportsbookClientPermission.SBC_OE.ToString().Replace("_", "."))
                                    {
                                        var sbsFirstClaim = UserClaims.FirstOrDefault(x => x.Type.ToUpper().Contains($"{SportsbookClientPermission.SBC_OE.ToString().Replace("_", ".").ToUpper()}."));

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
                                                    Url = $"hk/odds-engine{await GetPermissionLinks(sbsFirstClaim?.Type.ToString() ?? string.Empty)}",
                                                }
                                            }
                                        };

                                        NavMenus.Menus.Add(menu2);
                                        continue;
                                    }

                                    if (!string.IsNullOrEmpty(subMenu.GroupName) && subMenu.ClaimType.Contains(SportsbookClientPermission.SBC_OE.ToString().Replace("_", ".")))
                                        continue;

                                    var submenu1 = await MenuHelper.GetMenu(claim, subMenu.Name);
                                    menu.SubMenus.Add(submenu1);
                                }
                            }

                            if (menu.SubMenus.Any())
                                NavMenus.Menus.Add(menu);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                NavigationManager.NavigateTo("login", true);
            }
            finally
            {
                IsPreLoader = false;
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
                    Log.Error(ex.Message);
                    if (ex.StackTrace != null)
                        Log.Error(ex.StackTrace.ToString());

                    toasterService.AddToast(Toast.NewToast
                            ("Render Error", ex.Message));
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
            if (_hubConnection != null)
            {
                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }
            }

            Dispose();
        }

        [JSInvokable]
        public static async Task OnInvokeFilterEvent()
        {
            await OnFilterClientActionAsync.Invoke();
        }

        public async Task OnFilterClientEvent()
        {
            //await JSRuntime.InvokeVoidAsync("filterClientName", Clients.Clients.Select(c => c.ClientName).ToList());
        }

        public async Task AuthenticateAccess()
        {
            var authResponse = await AuthenticationService.AuthenticateToken();

            if (authResponse.StatusCode != StatusCode.Success)
            {
                Log.Warning($"[MainLayoutBase] [Unauthorized] {NavigationManager.Uri}");
                Log.Warning($"{authResponse.StatusCode} - {authResponse.Message}");
                NavigationManager.NavigateTo("login", true);
            }
            else
            {
                Log.Information($"[MainLayoutBase] [Authorized] {NavigationManager.Uri}");
            }
        }

        public void Dispose()
        {
            TranslationState.PropertyChanged -= async (s, e) => await InvokeAsync(StateHasChanged);

            toasterService.Dispose();
            _toasterService.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities
        private async Task Subscribe()
        {
            try
            {
                var request = _httpContextAccessor.HttpContext?.Request;

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
                        toasterService.AddToast(Toast.NewToast
                                ("Error Connecting to hub", "Fault in connecting."));
                    }
                    else if (task.IsCompletedSuccessfully)
                    {
                        UserModel.ConnectionId = _hubConnection.ConnectionId.ToString();
                    }
                });

                await _hubConnection.InvokeAsync("AddToGroup", "HK");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                toasterService.AddToast(Toast.NewToast
                        ("Error Connecting to hub", ex.Message));
            }
        }

        private async Task ProcessNotification(NotificationsModel message)
        {
            if (message != null)
            {
                if (!string.IsNullOrEmpty(message.UserId) && message.UserId != UserModel.Id)
                    return;

                toasterService.AddToast(Toast.NewToast
                        (message.Page, message.Details, true));
            }
        }

        private async Task<PermissionResponse> GetPermissionList()
        {
            var response = await AuthenticationService.GetPermissionList(false, true);

            //if (response.StatusCode == StatusCode.Success)
            //{
            //    return new PermissionResponse
            //    {
            //        Permissions = response.Result.Permissions ?? new List<PermissionModel>(),
            //    };
            //}

            //return new PermissionResponse();

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
        private async Task<string> GetPermissionLinks(string permission)
        {
            var result = string.Empty;

            if (permission.Equals("SBC.OE.Monitoring.Outrights", StringComparison.InvariantCultureIgnoreCase))
                result = string.Empty;
            else if (permission.Equals("SBC.OE.FastOdds", StringComparison.InvariantCultureIgnoreCase))
                result = string.Empty;
            else if (permission.Equals("SBC.OE.Config", StringComparison.InvariantCultureIgnoreCase))
                result = "/config";
            else if (permission.Equals("SBC.OE.SportsSettings", StringComparison.InvariantCultureIgnoreCase))
                result = "/sport-settings";
            else if (permission.Equals("SBC.OE.MarketSetting.SortOrder", StringComparison.InvariantCultureIgnoreCase))
                result = "/market-sort-order";
            else if (permission.Equals("SBC.OE.MarketSetting.Group", StringComparison.InvariantCultureIgnoreCase))
                result = "/market-groups";
            else if (permission.Equals("SBC.OE.PriceBoost.Manage", StringComparison.InvariantCultureIgnoreCase))
                result = "/manage-boost";
            else if (permission.Equals("SBC.OE.PriceBoost.Settings", StringComparison.InvariantCultureIgnoreCase))
                result = "/boosted-settings";

            return result;
        }
        #endregion
    }
}
