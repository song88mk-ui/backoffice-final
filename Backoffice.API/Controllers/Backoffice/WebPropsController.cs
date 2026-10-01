using Backoffice.API.ActionFilter;
using Backoffice.API.Repositories;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AuthorizeAttribute = Backoffice.API.ActionFilter.AuthorizeAttribute;
using BoStatusCode = Backoffice.Shared.Enums.StatusCode;

namespace Backoffice.API.Controllers.Backoffice
{
    [Authorize("Admin.ClientSiteConfig", ((int)Access.View))]
    [Route("api/config/feature-flags")]
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]
    public class WebPropsController : ControllerBase
    {
        private readonly IWebPropsRepository _webPropsRepository;

        public WebPropsController(IWebPropsRepository webPropsRepository)
        {
            _webPropsRepository = webPropsRepository;
        }

        [HttpGet("{clientId}")]
        public async Task<IActionResult> GetClientFeatureFlags(string clientId)
        {
            var result = await _webPropsRepository.GetClientFeatureFlagsAsync(clientId);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                case BoStatusCode.NotFound:
                    return NotFound(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("{clientId}")]
        [Authorize("Admin.ClientSiteConfig", ((int)Access.Edit))]
        public async Task<IActionResult> UpdateClientFeatureFlags(string clientId, [FromBody] UpdateClientFeatureFlagsRequest request)
        {
            request.ClientId = clientId;

            var result = await _webPropsRepository.UpdateClientFeatureFlagsAsync(request);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
    }
}
