using Backoffice.Shared.Enums;
using Backoffice.Shared.Extensions;
using Backoffice.Shared.Models;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.JSInterop;
using Serilog;
using System.Data;
using System.Text.Json;
using IAuthenticationService = Backoffice.Web.Services.IAuthenticationService;

namespace Backoffice.Web.Pages.Authentication
{
    public class LoginModel : PageModel
    {
        #region Properties

        [TempData] public string ErrorMessage { get; set; }
        [BindProperty] public LogInModel LogInModel { get; set; } = new LogInModel();
        [BindProperty] public bool IsSaveCookie { get; set; }

        public bool IsValidLogin { get; set; }
        public string ReturnUrl { get; set; }
        public string Error { get; set; }

        public Dictionary<string, string> Translations { get; set; }
        public List<LanguageCode> LanguageCodes { get; set; } = new List<LanguageCode>();

        #endregion

        #region Fields

        private IJSRuntime _jsRuntime;
        private readonly ITranslationService _translationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly List<string> _access = new List<string>() { PermissionCategory.SBC.ToString().ToUpper(), PermissionCategory.Admin.ToString().ToUpper() };


        #endregion

        #region Ctor

        public LoginModel(IAuthenticationService authenticationService, IHttpContextAccessor httpContextAccessor, IJSRuntime jSRuntime, ITranslationService translationService)
        {
            _authenticationService = authenticationService;
            _httpContextAccessor = httpContextAccessor;
            _jsRuntime = jSRuntime;
            _translationService = translationService;

            LanguageCodes = new List<LanguageCode> { LanguageCode.EN, LanguageCode.ZH, LanguageCode.KO };
        }

        #endregion

        #region Methods

        public async Task<IActionResult> OnGet(string? returnUrl = null)
        {
            var selectedLangCode = Request.Cookies["c-langCode"]?.ToString() ?? "EN";

            if (Translations == null || (!string.IsNullOrEmpty(selectedLangCode) && LogInModel.LanguageCode != selectedLangCode))
            {
                if (Enum.TryParse(selectedLangCode, out LanguageCode langCode))
                {
                    LogInModel.LanguageCode = selectedLangCode;

                    Translations = await _translationService.GetTranslationsAsync(langCode);

                    Response.Cookies.Delete("c-langCode");
                    Response.Cookies.Append("c-langCode", selectedLangCode);
                }
            }

            IsSaveCookie = false;

            var cookieToken = RetrieveToken();

            if (!string.IsNullOrEmpty(cookieToken))
            {
                try
                {
                    var policies = new List<string>();
                    TempData["HasError"] = false;

                    var preference = JsonSerializer.Deserialize<TokenModel>(cookieToken);

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
                            TempData["LogError"] = $"<script>console.log(\"{authenticate.StatusCode.ToString()} - {authenticate.Message} -\n{JsonSerializer.Serialize(LogInModel).Replace("\"", "")}\")</script>";

                            if (!string.IsNullOrEmpty(authenticate.Message) && !authenticate.Message.Contains("Incorrect password"))
                            {
                                ModelState.AddModelError(string.Empty, Translations.GetValue(authenticate.Message));
                            }
                            else
                            {
                                ModelState.AddModelError(string.Empty, Translations.GetValue("Username or Password is incorrect"));
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
        public async Task<IActionResult> OnPostTranslateLanguage(string languageCode)
        {
            await LoadTranslationsAsync(languageCode);

            return Page();
        }

        public async Task<IActionResult> OnPostLogin()
        {
            await LoadTranslationsAsync();

            if (ModelState.IsValid)
            {
                var policies = new List<string>();
                TempData["HasError"] = false;
                var result = await _authenticationService.Login(LogInModel);

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
                TempData["LogError"] = $"<script>console.log(\"{result.StatusCode.ToString()} - {result.Message} -\n{JsonSerializer.Serialize(LogInModel).Replace("\"", "")}\")</script>";

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

        public void StoreToken(string token)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMonths(1),
                };

                context.Response.Cookies.Append("BlUserToken", token, cookieOptions);
            }
        }
        public string RetrieveToken()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                if (context.Request.Cookies.TryGetValue("BlUserToken", out var token))
                    return token;
            }

            return string.Empty;
        }
        private void SetRememberMePreference(bool value, TokenModel tokenModel = null)
        {
            if (value && tokenModel != null)
            {
                var token = JsonSerializer.Serialize(tokenModel);

                //var options = new CookieOptions
                //{
                //    Expires = DateTime.UtcNow.AddMonths(1),
                //    SameSite = SameSiteMode.Lax,
                //    Secure = true
                //};

                //Response.Cookies.Append("RememberMePreference", token, options);
                StoreToken(token);
            }
            else
            {
                Response.Cookies.Delete("RememberMePreference");
            }

            IsSaveCookie = value;
        }
        private async Task LoadTranslationsAsync(string languageCode = null)
        {
            var langCodeCookie = Request.Cookies["c-langCode"]?.ToString() ?? "EN";
            var selectedLangCode = !string.IsNullOrEmpty(languageCode) ? languageCode : langCodeCookie;

            if (Translations == null || (!string.IsNullOrEmpty(selectedLangCode) && LogInModel.LanguageCode != selectedLangCode))
            {
                if (Enum.TryParse(selectedLangCode, out LanguageCode langCode))
                {
                    LogInModel.LanguageCode = selectedLangCode;

                    Translations = await _translationService.GetTranslationsAsync(langCode);

                    Response.Cookies.Delete("c-langCode");
                    Response.Cookies.Append("c-langCode", selectedLangCode);
                }
            }
        }

        #endregion
    }
}