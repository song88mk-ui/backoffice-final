using Backoffice.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IAuthenticationService = Backoffice.Web.Services.IAuthenticationService;

namespace Backoffice.Web.Pages.Authentication
{
    public class LoginModel : PageModel
    {
        [Inject]
        public NavigationManager NavigationManager { get; set; }
        [TempData] public string ErrorMessage { get; set; }

        [BindProperty] public UserModel UserModel { get; set; } = new UserModel();

        private readonly IAuthenticationService AuthenticationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public string ReturnUrl { get; set; }
        public string Error { get; set; }
        public LoginModel(IAuthenticationService authenticationService, IHttpContextAccessor httpContextAccessor)
        {
            AuthenticationService = authenticationService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> OnGet(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                TempData["HasError"] = true;
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            if (!string.IsNullOrEmpty(_httpContextAccessor.HttpContext.Session.GetString("ErrorMessage")))
            {
                TempData["HasError"] = true;
                ModelState.AddModelError(string.Empty, _httpContextAccessor.HttpContext.Session.GetString("ErrorMessage"));

                _httpContextAccessor.HttpContext.Session.Remove("ErrorMessage");
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            // auto login
            // Use refresh token

            ReturnUrl = returnUrl;

            return Page();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                TempData["HasError"] = false;
                var result = await AuthenticationService.Login(UserModel);
                if (result.Result)
                {
                    //var cookie = new HttpCookie("Login");
                    //cookie.Values.Add("EmailID", LoginUser.EmailID);
                    //cookie.Values.Add("Password", LoginUser.Password);
                    //cookie.Expires = DateTime.Now.AddDays(15);
                    //Response.Cookies.Add(cookie);

                    //NavigationManager.NavigateTo("/");
                    return LocalRedirect("/choose-access");
                }
                else
                {
                    if (UserModel != null)
                        UserModel.Password = String.Empty;

                    TempData["HasError"] = true;

                    if(result.Message.ToLower().Contains("user does not exist"))
                        ModelState.AddModelError("UserModel.Name", "Sorry, we couldn't find an account with that username, please try again.");

                    ModelState.AddModelError("UserModel.Password", "Password is incorrect, please try again or click Forgot Password to reset it.");
                }
            }
            return Page();
        }
    }
}
