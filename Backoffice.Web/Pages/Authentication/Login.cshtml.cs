using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;
using System.Security.Claims;
using System.Text.Json;
using IAuthenticationService = Backoffice.Web.Services.IAuthenticationService;

namespace Backoffice.Web.Pages.Authentication
{
    public class LoginModel : PageModel
    {
        #region Properties

        [Inject] public NavigationManager NavigationManager { get; set; }
        [TempData] public string ErrorMessage { get; set; }
        [BindProperty] public LogInModel UserModel { get; set; } = new LogInModel();
        [BindProperty] public bool IsSaveCookie { get; set; }

        public bool IsValidLogin { get; set; }
        public string ReturnUrl { get; set; }
        public string Error { get; set; }
        private IJSRuntime _jsRuntime;
        private readonly List<string> _access;

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
            _access = new List<string>() { PermissionCategory.SBC.ToString().ToUpper(), PermissionCategory.Admin.ToString().ToUpper() };
        }

        #endregion

        #region Methods

        public async Task<IActionResult> OnGet(string? returnUrl = null)
        {
            //IsSaveCookie = await GetRememberMePreference();
            IsSaveCookie = false;
            if (Request.Cookies.ContainsKey("RememberMePreference"))
            {
                try
                {
                    IsSaveCookie = true;
                    var policies = new List<string>();
                    TempData["HasError"] = false;
                    var preference = JsonSerializer.Deserialize<Tuple<LogInModel, TokenModel>>(Request.Cookies["RememberMePreference"]?.ToString());
                    UserModel = preference.Item1;
                    var tokenModel = preference.Item2;

                    if (tokenModel == null && UserModel == null)
                    {
                        IsSaveCookie = false;
                        Response.Cookies.Delete("RememberMePreference");
                    }
                    else
                    {
                        if (tokenModel?.AccessToken != null)
                        {
                            var authenticate = await _authenticationService.AuthenticateToken(tokenModel.AccessToken);
                            if (authenticate.StatusCode == Backoffice.Shared.Enums.StatusCode.Success)
                            {
                                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(JwtParser.ParseClaimsFromJwt(tokenModel.AccessToken), "jwtAuthType"));
                                policies = claimsPrincipal.Claims?.Where(x => x.Value != "0" && _access.Contains(x.Type.ToUpper()))
                                    .Select(x => x.Type.Split('.').FirstOrDefault())
                                    .Distinct()
                                    .ToList();
                                if (policies.Count == 1)
                                {
                                    switch (policies.FirstOrDefault().ToString().ToUpper())
                                    {
                                        case "ADMIN":
                                            return LocalRedirect("/admin/dashboard");
                                        case "SBC":
                                        case "PM":
                                            return LocalRedirect("/dashboard");
                                        //    return LocalRedirect("/pm/dashboard");
                                        default:
                                            return LocalRedirect("/choose-access");
                                    }
                                }
                                else if (policies.Count > 1)
                                {
                                    return LocalRedirect("/choose-access");
                                }
                            }

                            TempData["HasError"] = true;
                            TempData["LogError"] = $"<script>console.log(\"{authenticate.StatusCode.ToString()} - {authenticate.Message} -\n{JsonSerializer.Serialize(UserModel).Replace("\"", "")}\")</script>";

                            if (!string.IsNullOrEmpty(authenticate.Message) && !authenticate.Message.Contains("Incorrect password"))
                            {
                                ModelState.AddModelError(string.Empty, authenticate.Message);
                            }
                            else
                            {
                                ModelState.AddModelError(string.Empty, "Username or Password is incorrect");
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Response.Cookies.Delete("RememberMePreference");
                }
            }

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
                    if (IsSaveCookie)
                    {
                        // Store "Remember Me" preference in a cookie
                        var options = new CookieOptions
                        {
                            Expires = DateTime.UtcNow.AddMonths(1), // Example: Cookie expires in 1 month
                            SameSite = SameSiteMode.Lax,
                            Secure = true
                        };

                        Response.Cookies.Append("RememberMePreference", JsonSerializer.Serialize(new Tuple<LogInModel, TokenModel>(UserModel, result.Result.Item3)), options);
                        //_httpContextAccessor.HttpContext.Response.Cookies.Append("RememberMePreference", "true", options);
                    }
                    else
                        Response.Cookies.Delete("RememberMePreference");

                    policies = result.Result.Item2.Where(x => _access.Contains(x.ToUpper())).ToList();
                    if (policies.Count == 1)
                    {
                        switch (policies.FirstOrDefault().ToString().ToUpper())
                        {
                            case "ADMIN":
                                return LocalRedirect("/admin/dashboard");
                            case "SBC":
                            case "PM":
                                return LocalRedirect("/dashboard");
                            //    return LocalRedirect("/pm/dashboard");
                            default:
                                return LocalRedirect("/choose-access");
                        }
                    }
                    else if (policies.Count > 1)
                    {
                        return LocalRedirect("/choose-access");
                    }

                    NavigationManager.NavigateTo("/");
                }

                TempData["HasError"] = true;
                TempData["LogError"] = $"<script>console.log(\"{result.StatusCode.ToString()} - {result.Message} -\n{JsonSerializer.Serialize(UserModel).Replace("\"", "")}\")</script>";

                if (!string.IsNullOrEmpty(result.Message) && !result.Message.Contains("Incorrect password"))
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

        #region Utilities
        private async Task<bool> GetRememberMePreference()
        {
            try
            {
                var preference = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "RememberMePreference");
                return preference != null && bool.TryParse(preference, out bool result) ? result : false;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private async Task SetRememberMePreference(bool value)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "RememberMePreference", value.ToString().ToLower());
        }
        #endregion
    }
}
