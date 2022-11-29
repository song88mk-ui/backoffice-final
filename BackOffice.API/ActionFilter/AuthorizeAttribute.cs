using Backoffice.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;

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
            var response = (await client.GetAsync($"/validate{query}"));

            var payload = await response.Content.ReadAsStringAsync();
            var validToken = JsonConvert.DeserializeObject<BaseResponse<bool>>(payload);

            if (response.IsSuccessStatusCode)
            {
                if (!validToken.Result)
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
