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
            try
            {
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

            }
            catch(Exception ex)
            {

                context.Result = new JsonResult(new { title = "Unauthorized", message = ex.Message }) { StatusCode = StatusCodes.Status401Unauthorized };
                return;
            }
        }
    }
}
