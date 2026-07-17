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
    [Authorize("Admin.FeatureFlags", ((int)Access.View))]
    [Route("api/config/feature-flags")]
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]
    public class WebPropsController : ControllerBase
    {
        private readonly IWebPropsRepository _webPropsRepository;

        public WebPropsController(IWebPropsRepository webPropsRepository)
        {
            _webPropsRepository = webPropsRepository;
        }

        [HttpGet("{clientCode}")]
        public async Task<IActionResult> GetClientFeatureFlags(string clientCode)
        {
            var result = await _webPropsRepository.GetClientFeatureFlagsAsync(clientCode);

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

        [HttpPost("{clientCode}")]
        [Authorize("Admin.FeatureFlags", ((int)Access.Edit))]
        public async Task<IActionResult> UpdateClientFeatureFlags(string clientCode, [FromBody] UpdateClientFeatureFlagsRequest request)
        {
            request.ClientCode = clientCode;

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
