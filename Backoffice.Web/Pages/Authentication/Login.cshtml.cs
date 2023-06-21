using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;
using Serilog;
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
                    var policies = new List<string>();
                    TempData["HasError"] = false;

                    var preference = JsonSerializer.Deserialize<TokenModel>(Request.Cookies["RememberMePreference"]?.ToString());

                    if (preference.AccessToken == null)
                    {
                        SetRememberMePreference(false);
                    }
                    else
                    {
                        IsSaveCookie = true;
                        if (preference?.AccessToken != null)
                        {
                            var authenticate = await _authenticationService.AuthenticateToken(preference.AccessToken);
                            if (authenticate.StatusCode == Backoffice.Shared.Enums.StatusCode.Success)
                            {
                                var claimsPrincipal = JwtParser.ParseClaimsFromJwt(preference.AccessToken);
                                policies = claimsPrincipal?.Where(x => x.Value != "0" && _access.Contains(x.Type.Split(".")?.FirstOrDefault().ToUpper()))
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
                    SetRememberMePreference(false);

                    Log.Error(ex.Message);
                    if (ex.StackTrace != null)
                        Log.Error(ex.StackTrace.ToString());
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
                    SetRememberMePreference(IsSaveCookie, result.Result.Item3);
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
                    else if (!policies.Any())
                    {
                        TempData["HasError"] = true;
                        ModelState.AddModelError(string.Empty, "No permissions enabled for this user account. Contact admin.");
                        return Page();
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
        private void SetRememberMePreference(bool value, TokenModel token = null)
        {
            if (value && token != null)
            {
                var options = new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddMonths(1), // Example: Cookie expires in 1 month
                    SameSite = SameSiteMode.Lax,
                    Secure = true
                };
                Response.Cookies.Append("RememberMePreference", JsonSerializer.Serialize(token), options);
            }
            else
            {
                Response.Cookies.Delete("RememberMePreference");
            }

            IsSaveCookie = value;
        }
        #endregion
    }
}
