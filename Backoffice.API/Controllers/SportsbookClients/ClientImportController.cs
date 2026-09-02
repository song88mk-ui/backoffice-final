using Backoffice.API.ActionFilter;
using Backoffice.API.Repositories;
using Backoffice.Shared.Models.Requests;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Backoffice.API.Controllers.SportsbookClients
{
    [Authorize]
    [Route("sportsbook/[controller]")]
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]
    public class ClientImportController : Controller
    {
        private readonly IClientImportRepository _clientImportRepository;

        public ClientImportController(IClientImportRepository clientImportRepository)
        {
            _clientImportRepository = clientImportRepository;
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidateClientImportRequest request)
        {
            var result = await _clientImportRepository.ValidateBatch(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("commit")]
        public async Task<IActionResult> Commit([FromBody] CommitClientImportRequest request)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            var userName = HttpContext.Items["UserName"]?.ToString();

            var result = await _clientImportRepository.CommitBatch(request, userId, userName);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("environment")]
        public IActionResult GetEnvironment()
        {
            var result = _clientImportRepository.GetEnvironment();

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("progress/{batchId}")]
        public async Task<IActionResult> GetProgress(Guid batchId)
        {
            var result = await _clientImportRepository.GetProgress(batchId);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("running/{environment}")]
        public async Task<IActionResult> IsRunning(string environment)
        {
            var result = await _clientImportRepository.IsImportRunning(environment);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
    }
}
