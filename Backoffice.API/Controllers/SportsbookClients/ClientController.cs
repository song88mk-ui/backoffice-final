using Backoffice.API.ActionFilter;
using Backoffice.API.Repositories;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backoffice.API.Controllers.SportsbookClients
{
    [Authorize]
    [Route("sportsbook/[controller]")]
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]
    public class ClientController : Controller
    {
        private readonly IBackofficeRepository _backofficeRepository;
        private readonly ISportsbookRepository _sportsbookRepository;
        public ClientController(IBackofficeRepository backofficeRepository, ISportsbookRepository sportsbookRepository)
        {
            _backofficeRepository = backofficeRepository;
            _sportsbookRepository = sportsbookRepository;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetClients()
        {
            var result = await _backofficeRepository.GetClients();

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("information/list")]
        public async Task<IActionResult> GetClientList([FromBody] GetClientListRequest request)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var clientCode = JwtParser.ParseClaimsFromJwt(token).Where(x => x.Type.ToUpper() == "CLIENTCODES").FirstOrDefault()?.Value.ToString();

            var result = new BaseResponse<ClientListResponse>();
            if (clientCode.ToUpper() == "ALL")
                result = await _backofficeRepository.GetClientList(request);
            else
            {
                var brands = JwtParser.ParseClaimsFromJwt(token).Where(x => x.Type.ToUpper() == "CLIENTCODES").FirstOrDefault()?.Value.Split(',').ToList() ?? new List<string>();
                result = await _backofficeRepository.GetClientList(request, brands);
            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("post")]
        public async Task<IActionResult> AddOrUpdateClient([FromBody] ClientListResponse.Client request)
        {
            var result = await _backofficeRepository.AddOrUpdateClient(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("bet/setting")]
        public async Task<IActionResult> GetBetSetting([FromBody] BetSettingRequest request)
        {
            var result = await _backofficeRepository.GetBetSetting(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("exchange/rates")]
        public async Task<IActionResult> GetExchangeRates()
        {
            var result = await _backofficeRepository.GetExchangeRates();

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("platform/list")]
        public async Task<IActionResult> GetPlatformClients()
        {
            var result = await _backofficeRepository.GetPlatformClients();

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        #region  Market Group
        [HttpPost("markettemplategroups")]
        public async Task<IActionResult> GetClientMarketTemplateGroups(ClientMarketTemplateRequest request)
        {
            var result = await _sportsbookRepository.GetClientMarketTemplateGroups(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("markettemplategroup/post")]
        public async Task<IActionResult> AddOrUpdateClientMarketTemplateGroup([FromBody] ClientPostMarketTemplateGroupRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _sportsbookRepository.AddOrUpdateClientMarketTemplateGroup(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("markettemplategroup/delete")]
        public async Task<IActionResult> DeleteClientMarketTemplateGroup([FromBody] ClientDeleteMarketTemplateGroupRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _sportsbookRepository.DeleteClientMarketTemplateGroup(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("markettemplategroup/copy")]
        public async Task<IActionResult> CopyClientMarketTemplateGroup([FromBody] ClientCopyMarketTemplateGroupRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _sportsbookRepository.CopyClientMarketTemplateGroup(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        #endregion

        #region Sport Settings
        [HttpPost("sportsettings")]
        public async Task<IActionResult> AddOrUpdateClientSportSettings([FromBody] ClientSportSettingRequest request)
        {
            var result = await _sportsbookRepository.AddOrUpdateClientSportSettings(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("sportorder/list")]
        public async Task<IActionResult> GetClientSportOrderList([FromBody] ClientSportOrderListRequest request)
        {
            var result = await _sportsbookRepository.GetClientSportOrderList(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("sportorder/audit/list")]
        public async Task<IActionResult> GetClientSportOrderAuditList([FromBody] ClientSportOrderAuditRequest request)
        {
            var result = await _sportsbookRepository.GetClientSportOrderAuditList(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("segmentorder/list")]
        public async Task<IActionResult> GetClientSegmentOrderList([FromBody] ClientSegmentOrderListRequest request)
        {
            var result = await _sportsbookRepository.GetClientSegmentOrderList(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("leagueorder/list")]
        public async Task<IActionResult> GetClientLeagueOrderList([FromBody] ClientLeagueOrderListRequest request)
        {
            var result = await _sportsbookRepository.GetClientLeagueOrderList(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("oddstableleagueorder/list")]
        public async Task<IActionResult> GetClientOddstableLeagueOrderList([FromBody] ClientOddstableLeagueOrderListRequest request)
        {
            var result = await _sportsbookRepository.GetClientOddstableLeagueOrderList(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("oddstableleaguesettings")]
        public async Task<IActionResult> AddOrUpdateClientOddstableLeagueSettings([FromBody] ClientOddstableLeagueSettingRequest request)
        {
            var result = await _sportsbookRepository.AddOrUpdateClientOddstableLeagueSettings(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("oddstableleaguesettings/copy")]
        public async Task<IActionResult> CopyClientOddstableLeagueSettings([FromBody] CopyClientOddstableLeagueSettingRequest request)
        {
            var result = await _sportsbookRepository.CopyClientOddstableLeagueSettings(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("oddstableleaguesettings/logs")]
        public async Task<IActionResult> GetOddstableLeagueSortingLogs([FromBody] OddstableLeagueSortingLogRequest request)
        {
            var result = await _sportsbookRepository.GetOddstableLeagueSortingLogs(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("tier/leagueorder/list")]
        public async Task<IActionResult> GetTierLeagueOrderList()
        {
            var result = await _sportsbookRepository.GetTierLeagueOrderList();

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        #endregion

        #region Market Sort Order

        [HttpGet("markettemplate/{clientCode}/{sportId}/{fixtureStatusId}")]
        public async Task<IActionResult> GetClientMarketTemplates(string clientCode, int sportId, int fixtureStatusId)
        {
            var result = await _sportsbookRepository.GetClientMarketTemplates(clientCode, sportId, fixtureStatusId, "EN");

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("markettemplate")]
        public async Task<IActionResult> AddOrUpdateClientMarketTemplatesOrder([FromBody] ClientMarketTemplateRequest request)
        {
            var result = await _sportsbookRepository.AddOrUpdateClientMarketTemplatesOrder(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("markettemplate/copy")]
        public async Task<IActionResult> CopyClientMarketTemplatesOrder([FromBody] CopyClientMarketSortOrderRequest request)
        {
            var result = await _sportsbookRepository.CopyClientMarketTemplatesOrder(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        #endregion

        #region Selector Market Templates

        [HttpGet("selectormarkettemplate/{clientId:guid}")]
        public async Task<IActionResult> GetClientSelectorMarketTemplates(System.Guid clientId)
        {
            var result = await _sportsbookRepository.GetClientSelectorMarketTemplates(clientId);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("selectormarkettemplate/lookup")]
        public async Task<IActionResult> GetSelectorMarketTemplateLookup()
        {
            var result = await _sportsbookRepository.GetSelectorMarketTemplateLookup();

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("selectormarkettemplate")]
        public async Task<IActionResult> AddOrUpdateClientSelectorMarketTemplates([FromBody] ClientSelectorMarketTemplateRequest request)
        {
            var result = await _sportsbookRepository.AddOrUpdateClientSelectorMarketTemplates(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("selectormarkettemplate/copy")]
        public async Task<IActionResult> CopyClientSelectorMarketTemplates([FromBody] CopyClientSelectorMarketTemplateRequest request)
        {
            var result = await _sportsbookRepository.CopyClientSelectorMarketTemplates(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        #endregion

        #region Lookup - Sport/Segment/League
        [HttpGet("sports/lookup")]
        public async Task<IActionResult> GetSportLookup()
        {
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault())
                                                ? "EN"
                                                : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _sportsbookRepository.GetSportLookup(languageCode);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("segment/lookup/{sportId}")]
        public async Task<IActionResult> GetSegmentLookup(int sportId)
        {
            var result = await _sportsbookRepository.GetSegmentLookup(sportId);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("league/lookup/{sportId}/{segmentId}")]
        public async Task<IActionResult> GetLeagueLookup(int sportId, int segmentId)
        {
            var result = await _sportsbookRepository.GetLeagueLookup(sportId, segmentId);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        #endregion
    }
}
