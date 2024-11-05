using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using UAParser;
using Microsoft.Net.Http.Headers;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Utilities;
using System.IdentityModel.Tokens.Jwt;
using System;

namespace Backoffice.API.ActionFilter
{
    public class ApiKeyFilter : ActionFilterAttribute
    {
        private readonly IConfiguration _configuration;

        public ApiKeyFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var apiKey = context.HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var token = context.HttpContext.Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer", "").Trim();

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrWhiteSpace(apiKey) || _configuration["Security:ApiKey"] != apiKey)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    Title = "Unauthorized",
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message = "Missing Or Unsupported APIKey In Request Headers",
                });
                return;
            }

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

            var parsed = JwtParser.ParseClaimsFromJwt(token);

            context.HttpContext.Items["UserToken"] = token;
            context.HttpContext.Items["UserId"] = parsed.Where(x => x.Type.ToUpper() == "UserId".ToUpper()).FirstOrDefault()?.Value.ToString();
            context.HttpContext.Items["UserName"] = parsed.Where(x => x.Type.ToUpper() == "UserName".ToUpper()).FirstOrDefault()?.Value.ToString();
            context.HttpContext.Items["UserIdRef"] = parsed.Where(x => x.Type.ToUpper() == "UserIdRef".ToUpper()).FirstOrDefault()?.Value.ToString();
            context.HttpContext.Items["ClientCodes"] = parsed.Where(x => x.Type.ToUpper() == "ClientCodes".ToUpper()).FirstOrDefault()?.Value.ToString();
        }
    }
}
