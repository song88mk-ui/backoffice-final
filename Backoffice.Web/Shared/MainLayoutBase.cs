using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Backoffice.Web.Shared
{
    public class MainLayoutBase : LayoutComponentBase, IDisposable
    {
        #region Properties

        [Inject] public IConfiguration Configuration { get; set; }
        [Inject] public IHttpContextAccessor HttpContextAccessor { get; set; }

        [Inject] public NavigationManager NavigationManager { get; set; }
        [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        public UserModel UserModel { get; set; }
        public LogInModel Model { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                if (HttpContextAccessor.HttpContext.Session.GetString("UserToken") == null)
                {
                    NavigationManager.NavigateTo("login", true);
                }

                UserModel = new UserModel();

                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var userClaims = authState.User.Claims.ToList();

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
                        case "TOKENNAME":
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
            catch (Exception ex)
            {
                NavigationManager.NavigateTo("login", true);
            }
        }

        public async void Dispose()
        {
        }

        #endregion
    }
}
