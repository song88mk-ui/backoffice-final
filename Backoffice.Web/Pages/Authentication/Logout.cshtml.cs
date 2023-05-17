using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Backoffice.Web.Pages.Authentication
{
    public class LogoutModel : PageModel
    {
        [Inject] public NavigationManager NavigationManager { get; set; }

        private readonly IAuthenticationService _authenticationService;

        public LogoutModel(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public async Task<IActionResult> OnGet()
        {
            await _authenticationService.Logout();
            if (Request.Cookies.ContainsKey("RememberMePreference"))
            {
                var preference = JsonSerializer.Deserialize<Tuple<LogInModel, TokenModel>>(Request.Cookies["RememberMePreference"]?.ToString());
                var userModel = preference.Item1;
                Response.Cookies.Delete("RememberMePreference");

                if (userModel != null)
                {
                    var options = new CookieOptions
                    {
                        Expires = DateTime.UtcNow.AddMonths(1), // Example: Cookie expires in 1 month
                        SameSite = SameSiteMode.Lax,
                        Secure = true
                    };
                    Response.Cookies.Append("RememberMePreference", JsonSerializer.Serialize(new Tuple<LogInModel, TokenModel>(userModel, new TokenModel())), options);
                }
            }

            return LocalRedirect("~/login");
        }
    }
}
