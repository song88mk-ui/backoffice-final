using Backoffice.API.ActionFilter;
using Backoffice.API.Repositories;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoStatusCode = Backoffice.Shared.Enums.StatusCode;

namespace Backoffice.API.Controllers.Backoffice
{
    [Authorize("SBC.OE.Config", ((int)Access.View))]
    [Route("api/[controller]")]
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]
    public class ConfigController : ControllerBase
    {
        private readonly IBackofficeRepository _backofficeRepository;
        private readonly IMaintenanceRepository _maintenanceRepository;
        public ConfigController(IBackofficeRepository backofficeRepository, IMaintenanceRepository maintenanceRepository)
        {
            //
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

        [HttpGet("template/{id}")]
        public async Task<IActionResult> GetConfigTemplateById([FromRoute] long id)
        {
            var result = await _backofficeRepository.GetConfigTemplateById(id);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpGet("templates")]
        public async Task<IActionResult> GetConfigTemplates([FromQuery] int sportId = -1, [FromQuery] int scopeId = -1)
        {
            var result = await _backofficeRepository.GetConfigTemplates(sportId == -1 ? null : new List<int>() { sportId }, scopeId == -1 ? null : new List<int>() { scopeId });

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("template")]
        [Authorize("SBC.OE.Config", ((int)Access.Edit))]
        public async Task<IActionResult> AddOrUpdateConfigTemplate([FromBody] ConfigTemplateModel request)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();

            var result = await _backofficeRepository.AddOrUpdateConfigTemplate(request);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("template/set/{state}")]
        [Authorize("SBC.OE.Config", ((int)Access.Edit))]
        public async Task<IActionResult> ConfigTemplateSetState([FromBody] ConfigSetLeagueStateRequest request, [FromRoute] string state)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            var result = new BaseResponse<bool>()
            {
                StatusCode = BoStatusCode.Error,
                Message = "Invalid state type"
            };

            if (Enum.TryParse(state, out ConfigState confState))
            {
                result = await _backofficeRepository.SetConfigState(confState, request);
            }

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("template/delete/{id}")]
        [Authorize("SBC.OE.Config", ((int)Access.Edit))]
        public async Task<IActionResult> DeleteConfigTemplate([FromRoute] string id)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();

            var result = await _backofficeRepository.DeleteConfigTemplate(id);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("template/{state}")]
        public async Task<IActionResult> GetConfigClients([FromRoute] string state, [FromQuery] int templateId = -1, [FromBody] ConfigTemplateLeagueRequest payload = null)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            var clientCodes = HttpContext.Items["ClientCodes"]?.ToString().Split(',').ToList();

            var result = new BaseResponse<ConfigTemplateLeagueResponse>()
            {
                StatusCode = BoStatusCode.Error,
                Message = "Invalid state type"
            };

            if (templateId != -1)
            {
                result = await _backofficeRepository.GetConfigState(clientCodes, null, ConfigState.Assigned, templateId);
            }
            else
            {
                if (Enum.TryParse(state, out ConfigState confState))
                {
                    result = await _backofficeRepository.GetConfigState(clientCodes, payload, confState);
                }
            }

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpGet("template/count")]
        public async Task<IActionResult> GetTemplateConfigCount([FromQuery] string state = "")
        {
            var userId = HttpContext.Items["UserId"].ToString();
            var clientCodes = HttpContext.Items["ClientCodes"].ToString().Split(',').ToList();

            var result = await _backofficeRepository.GetTemplateConfigCount(clientCodes, state);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("template/margin/{id}")]
        public async Task<IActionResult> GetConfigTemplateMarginById([FromRoute] long id)
        {
            var result = await _backofficeRepository.GetConfigTemplateMarginById(id);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("template/margin")]
        [Authorize("SBC.OE.Config", ((int)Access.Edit))]
        public async Task<IActionResult> AddOrUpdateConfigTemplateMargin([FromBody] ConfigTemplateMarginModel request)
        {
            var result = await _backofficeRepository.AddOrUpdateConfigTemplateMargin(request);

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
