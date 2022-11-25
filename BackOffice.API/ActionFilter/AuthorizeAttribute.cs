using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net;
using System;
using Microsoft.Extensions.Configuration;
using Backoffice.Shared.Enums;
using System.Text.Json;
using Backoffice.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.IO;

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
            var token = context.HttpContext.Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");

            // Call Auth validator
            var client = new HttpClient()
            {
                BaseAddress = new Uri(_configuration["API:AuthenticationAPI:Uri"]),
            };

            client.DefaultRequestHeaders.Add("x-api-key", _configuration["API:AuthenticationAPI:Security:APIKey"]);

            var query = !string.IsNullOrWhiteSpace(token) ? "?token=" + token : "";
            var response = await client.GetAsync($"/validate{query}");

            var validToken = JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            if (response.IsSuccessStatusCode)
            {
                if(!validToken.Result)
                {
                    context.Result = new JsonResult(new { title = "Unauthorized", message = validToken.Message }) { StatusCode = StatusCodes.Status401Unauthorized };
                    return;
                }
            }
            else
            {
                context.Result = new JsonResult(new { title = "Unauthorized", message = validToken.Message }) { StatusCode = StatusCodes.Status401Unauthorized };
                return;
            }
        }
    }
}
