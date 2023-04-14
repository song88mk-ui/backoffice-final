using Backoffice.Shared.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IAuthenticationService = Backoffice.Web.Services.IAuthenticationService;

namespace Backoffice.Web.Pages.Authentication
{
    public class LoginModel : PageModel
    {
        #region Properties

        [Inject] public NavigationManager NavigationManager { get; set; }
        [TempData] public string ErrorMessage { get; set; }
        [BindProperty] public LogInModel UserModel { get; set; } = new LogInModel();

        public bool IsValidLogin { get; set; }
        public string ReturnUrl { get; set; }
        public string Error { get; set; }

        #endregion

        #region Fields

        private readonly IAuthenticationService _authenticationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        #endregion

        #region Ctor


        public LoginModel(IAuthenticationService authenticationService, IHttpContextAccessor httpContextAccessor)
        {
            _authenticationService = authenticationService;
            _httpContextAccessor = httpContextAccessor;
        }

        #endregion

        #region Methods

        public async Task<IActionResult> OnGet(string? returnUrl = null)
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
                var policies = new List<string>();
                TempData["HasError"] = false;
                var result = await _authenticationService.Login(UserModel);
                if (result.Result.Item1)
                {
                    policies = result.Result.Item2;
                    if (policies.Count == 1)
                    {
                        switch (policies.FirstOrDefault().ToString().ToUpper())
                        {
                            case "ADMIN":
                                return LocalRedirect("/admin/dashboard");
                            case "SBC":
                                return LocalRedirect("/dashboard");
                            //case "PM":
                            //    return LocalRedirect("/pm/dashboard");
                            default:
                                return LocalRedirect("/choose-access");
                        }
                    } else if (policies.Count > 1)
                    {
                        return LocalRedirect("/choose-access");
                    }

                    NavigationManager.NavigateTo("/");
                }

                TempData["HasError"] = true;

                //ModelState.AddFormError("Username or Password is incorrect");
                //ModelState.AddModelError("UserModel.Password", "Username or Password is incorrect");
                if(!string.IsNullOrEmpty(result.Message) && !result.Message.Contains("Incorrect password"))
                {
                    ModelState.AddModelError(string.Empty, result.Message);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Username or Password is incorrect");
                }
            }
            return Page();
        }

        #endregion
    }
}
