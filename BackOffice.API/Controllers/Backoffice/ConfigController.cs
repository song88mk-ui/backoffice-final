using Backoffice.API.ActionFilter;
using Backoffice.API.Repositories;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Metrics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoStatusCode = Backoffice.Shared.Enums.StatusCode;

namespace Backoffice.API.Controllers.Backoffice
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]
    public class ConfigController : ControllerBase
    {
        private readonly IBackofficeRepository _backofficeRepository;
        private readonly IMaintenanceRepository _maintenanceRepository;
        public ConfigController(IBackofficeRepository backofficeRepository, IMaintenanceRepository maintenanceRepository)
        {
            _backofficeRepository = backofficeRepository;
            _maintenanceRepository = maintenanceRepository;
        }

        [HttpPost("maintenance/{isMaintenance}")]
        public async Task<IActionResult> SetMaintenance([FromRoute] bool isMaintenance)
        {
            var result = await _maintenanceRepository.SetMaintenance(isMaintenance);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("maintenance")]
        public async Task<IActionResult> GetMaintenance()
        {
            var result = await _maintenanceRepository.GetMaintenance();

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("template/{sportId}/{fixtureStatusId}")]
        public async Task<IActionResult> GetClientConfigTemplate([FromRoute] int sportId = 0, [FromRoute] int fixtureStatusId = 0)
        {
            var userId = HttpContext.Items["UserId"].ToString();

            var result = await _backofficeRepository.GetConfigTemplate(userId, sportId, fixtureStatusId);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("template")]
        public async Task<IActionResult> AddOrUpdateConfigTemplate([FromBody] ConfigTemplateModel request)
        {
            var userId = HttpContext.Items["UserId"].ToString();

            var result = await _backofficeRepository.AddOrUpdateConfigTemplate(userId, request);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpDelete("template/{id}")]
        public async Task<IActionResult> DeleteConfigTemplate([FromRoute] string id)
        {
            var userId = HttpContext.Items["UserId"].ToString();

            var result = await _backofficeRepository.DeleteConfigTemplate(userId, id);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("template/{type}")] // type = assigned / unassigned
        public async Task<IActionResult> GetConfigClients([FromRoute] string type, [FromBody] MetaData metaData)
        {
            var userId = HttpContext.Items["UserId"].ToString();
            var clientCodes = HttpContext.Items["ClientCodes"].ToString().Split(',').ToList();

            var isAssigned = true;

            switch(type.ToLower())
            {
                case "assigned":
                case "unassigned":
                    isAssigned = type.ToLower() == "assigned";
                    break;
                default:
                    return BadRequest(new BaseResponse<List<ConfigClientModel>>() { StatusCode = BoStatusCode.Error, Message = "Invalid template type", Result = null });
            }    

            var result = await _backofficeRepository.GetConfigClient(userId, clientCodes, isAssigned);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        #region ActivityLog
        [HttpPost("activitylog")]
        public async Task<IActionResult> ActivityLog([FromBody] ActivityLogModel request)
        {
            var result = await _backofficeRepository.AddActivityLog(request);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("activitylog/list")]
        public async Task<IActionResult> GetActivityLogs([FromBody] ActivityLogRequest request)
        {
            var result = await _backofficeRepository.GetActivityLogs(request);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("activitylog/priceboostlist")]
        public async Task<IActionResult> GetPriceboostActivityLogs([FromBody] PriceboostActivityLogRequest request)
        {
            var result = await _backofficeRepository.GetPriceboostActivityLogs(request);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("activitylog/manualbetsettlementlist")]
        public async Task<IActionResult> GetManualBetSettlementHistoryLogs([FromBody] ManualBetSettlementActivityLogRequest request)
        {
            var result = await _backofficeRepository.GetManualBetSettlementHistoryLogs(request);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        #endregion
    }
}
