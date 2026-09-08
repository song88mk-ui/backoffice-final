using Backoffice.API.ActionFilter;
using Backoffice.API.Models;
using Backoffice.API.Repositories;
using Backoffice.API.Telemetry;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AllowAnonymousAttribute = Backoffice.API.ActionFilter.AllowAnonymousAttribute;
using AuthorizeAttribute = Backoffice.API.ActionFilter.AuthorizeAttribute;
using BoStatusCode = Backoffice.Shared.Enums.StatusCode;

namespace Backoffice.API.Controllers.Backoffice
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]
    public class ConfigController : ControllerBase
    {
        private readonly IBackofficeRepository _backofficeRepository;
        private readonly ActivitySource _activitySource;
        public ConfigController(IBackofficeRepository backofficeRepository, Instrumentation instrumentation)
        {
            _backofficeRepository = backofficeRepository;
            _activitySource = instrumentation.ActivitySource;
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
        public async Task<IActionResult> GetConfigTemplates([FromQuery] string searchText = "", [FromQuery] int sportId = -1, [FromQuery] int scopeId = -1)
        {
            var result = await _backofficeRepository.GetConfigTemplates(searchText, sportId == -1 ? null : new List<int>() { sportId }, scopeId == -1 ? null : new List<int>() { scopeId });

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
            var userName = HttpContext.Items["UserName"]?.ToString();

            var requester = new RequesterInfo()
            {
                UserId = userId,
                UserName = userName,
                Page = HttpContext.Request.Headers["x-page"].FirstOrDefault()
            };

            var result = await _backofficeRepository.DeleteConfigTemplate(id, requester);

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
            var languageCode = HttpContext.Items["x-language-code"]?.ToString();

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
                if (Enum.TryParse(state, ignoreCase: true, out ConfigState confState))
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
            using (var activity = _activitySource.StartActivity("template/count"))
            {
                var userToken = HttpContext.Items["UserToken"].ToString();
                activity?.SetTag("authorization", userToken);

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

        [HttpPost("template/copy/client")]
        [Authorize("SBC.OE.Config", ((int)Access.View))]
        public async Task<IActionResult> CopyClientConfig([FromBody] CopyClientConfigRequest request)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();

            var result = await _backofficeRepository.CopyClientConfig(request);

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
        [HttpPost("set/activitylog/list")]
        public async Task<IActionResult> SetActivityLogList([FromBody] ActivityLogListRequest request)
        {
            var result = await _backofficeRepository.AddActivityLogList(request);

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

        #region Translation

        [HttpPost("cachepreload")]
        [AllowAnonymous]
        public async Task<IActionResult> CachePreload([FromBody] OnDeleteCacheMessage request)
        {
            if (request == null)
            {
                return BadRequest("Request is required.");
            }

            if (!request.PurgeAll)
            {
                if (request.CacheType == CacheType.DynamicTranslation)
                {
                    if (request.CacheKeys == null || !request.CacheKeys.Any())
                    {
                        return BadRequest(
                            "CacheKeys is required for DynamicTranslation when InvalidateAll is false.");
                    }

                    if (!Enum.IsDefined(typeof(LanguageCode), request.LanguageCode))
                    {
                        return BadRequest(
                            "LanguageCode is required for DynamicTranslation.");
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(request.CacheKey))
                    {
                        return BadRequest(
                            "CacheKey is required when InvalidateAll is false.");
                    }
                }
            }


            var result = await _backofficeRepository.CachePreload(request);

            return Ok(new
            {
                Success = true,
                Message = "Cache invalidation request published successfully.",
            });
        }

        [AllowAnonymous]
        [HttpGet("translations")]
        public async Task<IActionResult> GetTranslations()
        {
            var userId = HttpContext.Items["UserId"]?.ToString();

            var langCode = LanguageCode.EN;
            Enum.TryParse(HttpContext.Request.Headers["x-language-code"].ToString(), out langCode);

            var result = await _backofficeRepository.GetTranslationsByLanguageCode(langCode);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        // Sportsbook Static/Dynamic Translation endpoints moved to TranslationController --
        // see that file's header comment for why (class-level [Authorize] here would still
        // run and reject requests before a method-level override on a different claim type
        // ever gets a chance to).

        #endregion
    }
}
