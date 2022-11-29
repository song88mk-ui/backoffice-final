using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Microsoft.AspNetCore.Http;

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
        }
    }
}
