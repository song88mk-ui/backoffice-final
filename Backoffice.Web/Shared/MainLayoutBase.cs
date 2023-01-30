using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.Shared.OddsEngine;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Backoffice.Web.Shared
{
    public class MainLayoutBase : LayoutComponentBase, IDisposable
    {
        #region Properties

        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IConfiguration Configuration { get; set; }
        [Inject] public IHttpContextAccessor HttpContextAccessor { get; set; }
        [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        [Parameter] public SportClickCallbackModel SelectedSport { get; set; }

        public HeaderNavMenu HeaderNavMenuReference { get; set; }

        public UserModel UserModel { get; set; } = new UserModel();
        public LogInModel Model { get; set; }

        public EventCallback<SportClickCallbackModel> OnSportClickCallback { get; set; }
        public bool IsPreLoader { get; set; } = true;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
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
                StateHasChanged();
            }
            catch (Exception)
            {
                NavigationManager.NavigateTo("login", true);
            }
        }

        public void OnSportClickEvent(SportClickCallbackModel model)
        {
            SelectedSport = model;
            HeaderNavMenuReference.ActivateMenu("monitoring");
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
