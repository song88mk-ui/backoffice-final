using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backoffice.API.ActionFilter
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private IConfiguration _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables().Build();
        private HttpClient _httpClient { get; set; }
        private Claim _requiredClaim;

        public AuthorizeAttribute(string claimType, int claimValue) : this()
        {
            _requiredClaim = new Claim(claimType, claimValue.ToString());
        }
        public AuthorizeAttribute()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_configuration["API:AuthenticationAPI:Uri"]);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _configuration["API:AuthenticationAPI:Security:APIKey"]);
        }
        public async void OnAuthorization(AuthorizationFilterContext context)
        {
            try
            {
                var region = string.Empty;
                if (context.HttpContext.Request.Headers.ContainsKey("x-region"))
                    region = context.HttpContext.Request.Headers["x-region"];

                if (context.HttpContext.Request.Headers.ContainsKey("x-language-code"))
                    context.HttpContext.Items["LanguageCode"] = context.HttpContext.Request.Headers["x-language-code"];

                if (context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any())
                    return;

                if (!context.HttpContext.Request.Headers.ContainsKey("Authorization"))
                {
                    context.Result = new JsonResult(new { title = "Unauthorized", message = "Missing authorization" }) { StatusCode = StatusCodes.Status401Unauthorized };
                    return;
                }

                var token = context.HttpContext.Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer", "").Trim();

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadToken(token);

                if (jwtToken == null)
                {
                    context.Result = new JsonResult(new { title = "Unauthorized", message = "Invalid token." }) { StatusCode = StatusCodes.Status401Unauthorized };
                    return;
                }

                // Invalid Isuer
                if (jwtToken.Issuer != _configuration["API:AuthenticationAPI:JWT:Issuer"])
                {
                    context.Result = new JsonResult(new { title = "Unauthorized", message = "Invalid token." }) { StatusCode = StatusCodes.Status401Unauthorized };
                    return;
                }

                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    context.Result = new JsonResult(new { title = "Unauthorized", message = "Invalid token." }) { StatusCode = StatusCodes.Status401Unauthorized };
                    return;
                }

                var result = new BaseResponse<TokenModel>()
                {
                    StatusCode = StatusCode.Unauthorized,
                    Message = "Missing authorization"
                };

                result = Authenticate(token).Result;

                if (result.StatusCode != StatusCode.Success)
                {
                    context.Result = new JsonResult(new { title = "Unauthorized", message = result.Message }) { StatusCode = StatusCodes.Status401Unauthorized };
                    return;
                }

                var parsed = JwtParser.ParseClaimsFromJwt(token);
                if (_requiredClaim != null)
                {
                    var validClaim = parsed.FirstOrDefault(x => x.Type.ToUpper() == _requiredClaim.Type.ToUpper());

                    if (validClaim == null)
                    {
                        context.Result = new JsonResult(new { title = "Unauthorized", message = result.Message }) { StatusCode = StatusCodes.Status401Unauthorized };
                        return;
                    }

                    if (validClaim.Value == ((int)Access.NoAccess).ToString())
                    {
                        context.Result = new JsonResult(new { title = "Unauthorized", message = result.Message }) { StatusCode = StatusCodes.Status401Unauthorized };
                        return;
                    }
                    if (_requiredClaim.Value == ((int)Access.Edit).ToString() && (validClaim.Value == ((int)Access.View).ToString()))
                    {
                        context.Result = new JsonResult(new { title = "Unauthorized", message = result.Message }) { StatusCode = StatusCodes.Status401Unauthorized };
                        return;
                    }
                }

                context.HttpContext.Items["UserToken"] = token;
                context.HttpContext.Items["UserId"] = parsed.Where(x => x.Type.ToUpper() == "UserId".ToUpper()).FirstOrDefault()?.Value.ToString();
                context.HttpContext.Items["UserName"] = parsed.Where(x => x.Type.ToUpper() == "UserName".ToUpper()).FirstOrDefault()?.Value.ToString();
                context.HttpContext.Items["UserIdRef"] = parsed.Where(x => x.Type.ToUpper() == "UserIdRef".ToUpper()).FirstOrDefault()?.Value.ToString();
                context.HttpContext.Items["ClientCodes"] = parsed.Where(x => x.Type.ToUpper() == "ClientCodes".ToUpper()).FirstOrDefault()?.Value.ToString();
            }
            catch (Exception ex)
            {
                context.Result = new JsonResult(new { title = "Unauthorized", message = ex.Message }) { StatusCode = StatusCodes.Status401Unauthorized };
                return;
            }
        }
        #region Utilities

        public async Task<BaseResponse<TokenModel>> Authenticate(string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var response = await _httpClient.PostAsync($"v1/authentication/access", new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<TokenModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<TokenModel>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized)
                        return JsonSerializer.Deserialize<BaseResponse<TokenModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<TokenModel>();
                    else
                    {
                        return new BaseResponse<TokenModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<TokenModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class AllowAnonymousAttribute : Attribute
    { }
}
