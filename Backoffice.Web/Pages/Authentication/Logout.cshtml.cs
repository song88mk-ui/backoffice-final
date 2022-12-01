using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Backoffice.Web.Pages.Authentication
{
    public class LogoutModel : PageModel
    {
        private readonly IAuthenticationService AuthenticationService;
        [Inject]
        public NavigationManager NavigationManager { get; set; }
        public LogoutModel(IAuthenticationService authenticationService)
        {
            AuthenticationService = authenticationService;
        }
        public async Task<IActionResult> OnGet()
        {
            await AuthenticationService.Logout();

            return LocalRedirect("~/login");
        }
    }
}
