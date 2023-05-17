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
                Response.Cookies.Delete("RememberMePreference");
            }

            return LocalRedirect("~/login");
        }
    }
}
