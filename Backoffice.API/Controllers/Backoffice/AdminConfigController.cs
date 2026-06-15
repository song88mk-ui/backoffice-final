using Backoffice.API.ActionFilter;
using Backoffice.API.Repositories;
using Backoffice.API.Telemetry;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthorizeAttribute = Backoffice.API.ActionFilter.AuthorizeAttribute;
using BoStatusCode = Backoffice.Shared.Enums.StatusCode;

namespace Backoffice.API.Controllers.Backoffice
{
    [Authorize("Admin.Maintenance", ((int)Access.View))]
    [Route("api/config")]
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]
    public class AdminConfigController : ControllerBase
    {
        private readonly IMaintenanceRepository _maintenanceRepository;

        public AdminConfigController(IMaintenanceRepository maintenanceRepository)
        {
            _maintenanceRepository = maintenanceRepository;
        }

        [HttpGet("maintenance")]
        public async Task<IActionResult> GetMaintenanceV2()
        {
            var clientCodes = HttpContext.Items["ClientCodes"]?.ToString()?.Split(',').ToList();

            var result = await _maintenanceRepository.GetMaintenanceV2(clientCodes);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("maintenance")]
        [Authorize("Admin.Maintenance", ((int)Access.Edit))]
        public async Task<IActionResult> SetMaintenanceV2([FromBody] SetMaintenanceRequest request)
        {
            var clientCodes = HttpContext.Items["ClientCodes"]?.ToString()?.Split(',').ToList();

            var result = await _maintenanceRepository.SetMaintenanceV2(request, clientCodes);

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
