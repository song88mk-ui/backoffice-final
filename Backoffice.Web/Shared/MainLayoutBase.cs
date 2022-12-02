using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace Backoffice.Web.Shared
{
    public class MainLayoutBase : LayoutComponentBase, IDisposable
    {
        [Inject] public IConfiguration Configuration { get; set; }
        [Inject] public IHttpContextAccessor HttpContextAccessor { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        public UserModel UserModel { get; set; }

        public LogInModel Model { get; set; }

        protected override async Task OnInitializedAsync()
        {
            UserModel = new UserModel();
            try
            {
                if (HttpContextAccessor.HttpContext.Session.GetString("UserToken") == null)
                {
                    NavigationManager.NavigateTo("/login", true);
                    // remove cascade value
                }

                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var userClaims = authState.User.Claims.ToList();
                UserModel = new UserModel()
                {
                    ClientCodes = new List<string>(),
                    Claims = new List<System.Security.Claims.Claim>()
                };

                foreach (var claim in userClaims)
                {
                    switch (claim.Type.ToUpper())
                    {
                        case "CLIENTCODES":
                            UserModel.ClientCodes.AddRange(claim.Value.Split(',').ToList());
                            break;
                        case "USERID":
                            UserModel.Id = claim.Value;
                            break;
                        case "USERNAME":
                            UserModel.UserName = claim.Value;
                            break;
                        default:
                            UserModel.Claims.Add(claim);
                            break;
                    }

                }


                // * Create Cascading parameter for user model
                // ClaimsPrincipal
                // ID
                // List of clients
            }
            catch (Exception)
            {
                NavigationManager.NavigateTo("/login", true);
            }
        }

        public async void Dispose()
        {
        }
    }
}
