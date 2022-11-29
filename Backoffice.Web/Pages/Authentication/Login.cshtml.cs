using Backoffice.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Backoffice.Web.Pages.Authentication
{
    public class LoginModel : PageModel
    {
        [BindProperty] public UserModel UserModel { get; set; }
        public void OnGet()
        {
        }
    }
}
