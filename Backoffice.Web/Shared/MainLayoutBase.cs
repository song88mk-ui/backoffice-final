using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.Shared.OddsEngine;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

namespace Backoffice.Web.Shared
{
    public class MainLayoutBase : LayoutComponentBase, IAsyncDisposable, IDisposable
    {
        #region Properties
        [Inject] private ToastService? _toasterService { get; set; }
        [Inject] private IHttpContextAccessor _httpContextAccessor { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
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
                var permissions = new List<string>() { "CLIENTCODES", "USERID", "TOKENNAME", "MAINUSERID" };

                if (userClaims is not null)
                {
                    if (isSuperAdmin)
                        UserModel.ClientCodes = clients.Clients.Select(c => c.ClientCode).ToList();
                    else
                        UserModel.ClientCodes = userClaims.Where(x => x.Type.ToUpper() == "CLIENTCODES").FirstOrDefault()?.Value.Split(',').ToList() ?? new List<string>();

                    UserModel.Id = userClaims.Where(x => x.Type.ToUpper() == "USERID").FirstOrDefault()?.Value ?? "";
                    UserModel.UserName = userClaims.Where(x => x.Type.ToUpper() == "TOKENNAME").FirstOrDefault()?.Value ?? "";
                    UserModel.MainUserId = userClaims.Where(x => x.Type.ToUpper() == "MAINUSERID").FirstOrDefault()?.Value ?? "";
                    UserModel.Claims = userClaims.Where(x => !permissions.Contains(x.Type.ToUpper())).ToList();
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

        #endregion
    }
}
