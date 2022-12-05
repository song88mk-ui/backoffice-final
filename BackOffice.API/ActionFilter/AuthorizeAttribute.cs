using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Utilities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace Backoffice.API.ActionFilter
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private IConfiguration _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();

        public async void OnAuthorization(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                context.Result = new JsonResult(new { title = "Unauthorized", message = "Missing authorization" }) { StatusCode = StatusCodes.Status401Unauthorized };
                return;
            }

            var token = context.HttpContext.Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token);

            if (jwtToken == null)
            {
                context.Result = new JsonResult(new { title = "Unauthorized", message = "Invalid token." }) { StatusCode = StatusCodes.Status401Unauthorized };
            }

            // Invalid Isuer
            if (jwtToken.Issuer != _configuration["JWT:Issuer"])
            {
                context.Result = new JsonResult(new { title = "Unauthorized", message = "Invalid Issuer." }) { StatusCode = StatusCodes.Status401Unauthorized };
            }

            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                // Call Auth validator
                var client = new HttpClient()
                {
                    BaseAddress = new Uri(_configuration["API:AuthenticationAPI:Uri"]),
                };

                client.DefaultRequestHeaders.Add("x-api-key", _configuration["API:AuthenticationAPI:Security:APIKey"]);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = (await client.GetAsync($"/authenticate"));

                var payload = await response.Content.ReadAsStringAsync();
                var validToken = JsonConvert.DeserializeObject<BaseResponse<TokenModel>>(payload);

                if (response.IsSuccessStatusCode)
                {
                    if (validToken.Result == null)
                    {
                        context.Result = new JsonResult(new { title = "Unauthorized", message = validToken.Message }) { StatusCode = StatusCodes.Status401Unauthorized };
                        return;
                    }
                }
                else
                {
                    context.Result = new JsonResult(new { title = "Unauthorized", message = "" }) { StatusCode = StatusCodes.Status401Unauthorized };
                    return;
                }
            }

        }
    }
}
