using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

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

        public UserModel UserModel { get; set; }
        public LogInModel Model { get; set; }

        public EventCallback<SportClickCallbackModel> OnSportClickCallback { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                UserModel = new UserModel();
                if (HttpContextAccessor.HttpContext.Session.GetString("UserToken") == null)
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
                Parallel.ForEach(userClaims, (claim) =>
                {
                    tasks.Add(Task.Run(() =>
                    {
                        switch (claim.Type.ToUpper())
                        {
                            case "CLIENTCODES":
                                if (isSuperAdmin)
                                    UserModel.ClientCodes.AddRange(clients.Clients.Select(c => c.ClientCode).ToList());
                                else
                                    UserModel.ClientCodes.AddRange(claim.Value.Split(',').ToList());
                                break;
                            case "USERID":
                                UserModel.Id = claim.Value;
                                break;
                            case "TOKENNAME":
                                UserModel.UserName = claim.Value;
                                break;
                            case "MAINUSERID":
                                UserModel.MainUserId = claim.Value;
                                break;
                            default:
                                UserModel.Claims.Add(claim);
                                break;
                        }
                    }));
                });

                Task.WaitAll(tasks.ToArray());
                StateHasChanged();
            }
            catch (Exception)
            {
                NavigationManager.NavigateTo("login", true);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            //if (firstRender)
            //{
            //}
        }

        public async Task OnSportClickEvent(SportClickCallbackModel model)
        {
            SelectedSport = model;
        }

        public async void Dispose()
        {
        }

        #endregion
    }
}
