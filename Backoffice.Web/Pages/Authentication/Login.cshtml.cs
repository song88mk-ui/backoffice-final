using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Utilities;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.JSInterop;
using Serilog;
using System.ComponentModel;
using System.Data;
using System.Text.Json;
using IAuthenticationService = Backoffice.Web.Services.IAuthenticationService;
namespace Backoffice.Web.Pages.Authentication
{
    public class LoginModel : PageModel
    {
        #region Properties

        [TempData] public string ErrorMessage { get; set; }
        [BindProperty] public LogInModel UserModel { get; set; } = new LogInModel();
        [BindProperty] public bool IsSaveCookie { get; set; }
        [FromQuery] public string LanguageCode { get; set; }

        public bool IsValidLogin { get; set; }
        public string ReturnUrl { get; set; }
        public string Error { get; set; }
        private IJSRuntime _jsRuntime;
        private readonly List<string> _access;

        #endregion

        #region Fields
        private readonly IAuthenticationService _authenticationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public readonly TranslationState _translationState;

        #endregion

        #region Ctor

        public LoginModel(IAuthenticationService authenticationService, IHttpContextAccessor httpContextAccessor, TranslationState translationState, IJSRuntime jSRuntime)
        {
            _authenticationService = authenticationService;
            _httpContextAccessor = httpContextAccessor;
            _access = new List<string>() { PermissionCategory.SBC.ToString().ToUpper(), PermissionCategory.Admin.ToString().ToUpper() };
            _translationState = translationState;

            _jsRuntime = jSRuntime;

            _translationState.PropertyChanged += TranslationStatePropertyChanged;
        }

        #endregion

        #region Methods
        public async Task OnEdit()
        {

        }
        public async Task<IActionResult> OnGet(string? returnUrl = null)
        {
            if (Enum.TryParse(LanguageCode, out LanguageCode langCode))
            {
                if (_translationState.ActiveLanguageCode != langCode)
                    _translationState.ActiveLanguageCode = langCode;
            }

            WelcomeTo = _translationState.GetValue("Welcome to");
            ViewData["back_office"] = _translationState.GetValue("Back Office");
            ViewData["login"] = _translationState.GetValue("Login");

            ViewData["username"] = _translationState.GetValue("Username");
            ViewData["enter_your_username"] = _translationState.GetValue("Enter your username");
            ViewData["password"] = _translationState.GetValue("Password");
            ViewData["enter_your_password"] = _translationState.GetValue("Enter your password");
            ViewData["remember_information"] = _translationState.GetValue("Remember Information");


            IsSaveCookie = false;
            if (Request.Cookies.ContainsKey("RememberMePreference"))
            {
                try
                {
                    var policies = new List<string>();
                    TempData["HasError"] = false;

                    var preference = JsonSerializer.Deserialize<TokenModel>(Request.Cookies["RememberMePreference"]?.ToString());

                    if (preference?.AccessToken == null)
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
            var action = Request.Form["action"];
            if (action == "login")
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

                        //_navigationManager.NavigateTo("/");
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
            }
            else
            {
                if (_translationState.ActiveLanguageCode.ToString() == action)
                    return Page();

                if (Enum.TryParse(action, out LanguageCode langCode))
                {
                    if (_translationState.ActiveLanguageCode != langCode)
                        _translationState.ActiveLanguageCode = langCode;
                }
            }
            return Page();
        }

        public string WelcomeTo { get; set; }
        private void TranslationStatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            WelcomeTo = _translationState.GetValue("Welcome to");
            ViewData["back_office"] = _translationState.GetValue("Back Office");
            ViewData["login"] = _translationState.GetValue("Login");

            ViewData["username"] = _translationState.GetValue("Username");
            ViewData["enter_your_username"] = _translationState.GetValue("Enter your username");
            ViewData["password"] = _translationState.GetValue("Password");
            ViewData["enter_your_password"] = _translationState.GetValue("Enter your password");
            ViewData["remember_information"] = _translationState.GetValue("Remember Information");
            
            //RedirectToPage("/", new { LanguageCode = _translationState.ActiveLanguageCode.ToString() });
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
