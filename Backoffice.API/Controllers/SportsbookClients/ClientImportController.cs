using Backoffice.API.ActionFilter;
using Backoffice.API.Repositories;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Utilities;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
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

        // search and sortBy are the Manage Clients list's own filter, so the file holds what the list shows.
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string search, [FromQuery] ClientSorting sortBy = ClientSorting.SortByBusinessNameAsc)
        {
            var result = await _clientImportRepository.ExportClients(search, sortBy, GetCallerBrands());

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("validate-update")]
        public async Task<IActionResult> ValidateUpdate([FromBody] ValidateClientUpdateRequest request)
        {
            var result = await _clientImportRepository.ValidateUpdateBatch(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("commit-update")]
        public async Task<IActionResult> CommitUpdate([FromBody] CommitClientUpdateRequest request)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            var userName = HttpContext.Items["UserName"]?.ToString();

            var result = await _clientImportRepository.CommitUpdateBatch(request, userId, userName);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("batches")]
        public async Task<IActionResult> GetBatchHistory([FromQuery] string operationType)
        {
            var result = await _clientImportRepository.GetBatchHistory(operationType);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("fieldchanges/{batchId}")]
        public async Task<IActionResult> GetFieldChanges(Guid batchId)
        {
            var result = await _clientImportRepository.GetFieldChanges(batchId);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        // Same rule as the client list (ClientController.GetClientList): a CLIENTCODES claim of "ALL" sees every
        // brand, anything else only the listed ones. A missing claim sees nothing rather than everything.
        private List<string> GetCallerBrands()
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var clientCodes = JwtParser.ParseClaimsFromJwt(token).FirstOrDefault(x => x.Type.ToUpper() == "CLIENTCODES")?.Value ?? string.Empty;

            if (string.Equals(clientCodes.Trim(), "ALL", StringComparison.OrdinalIgnoreCase))
                return null;

            return clientCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
    }
}
